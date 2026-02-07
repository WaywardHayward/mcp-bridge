using McpBridge.Models.Logging;
using Microsoft.Data.Sqlite;

namespace McpBridge.Services.Logging;

/// <summary>
/// SQLite-based invocation logger.
/// </summary>
public class SqliteInvocationLogger : IInvocationLogger, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SqliteInvocationLogger()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".mcp-bridge",
            "logs.db");
        
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS invocations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp TEXT NOT NULL,
                server_name TEXT NOT NULL,
                tool_name TEXT NOT NULL,
                parameters TEXT,
                success INTEGER NOT NULL,
                duration_ms INTEGER NOT NULL,
                response_summary TEXT,
                error TEXT
            );
            
            CREATE INDEX IF NOT EXISTS idx_invocations_timestamp 
                ON invocations(timestamp DESC);
            
            CREATE INDEX IF NOT EXISTS idx_invocations_server_tool 
                ON invocations(server_name, tool_name);
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task LogAsync(InvocationLog log, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO invocations 
                    (timestamp, server_name, tool_name, parameters, success, duration_ms, response_summary, error)
                VALUES 
                    (@timestamp, @serverName, @toolName, @params, @success, @durationMs, @response, @error)
                """;
            
            cmd.Parameters.AddWithValue("@timestamp", log.Timestamp.ToString("O"));
            cmd.Parameters.AddWithValue("@serverName", log.ServerName);
            cmd.Parameters.AddWithValue("@toolName", log.ToolName);
            cmd.Parameters.AddWithValue("@params", (object?)log.Parameters ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@success", log.Success ? 1 : 0);
            cmd.Parameters.AddWithValue("@durationMs", log.DurationMs);
            cmd.Parameters.AddWithValue("@response", (object?)log.ResponseSummary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@error", (object?)log.Error ?? DBNull.Value);
            
            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<InvocationLog>> GetLogsAsync(int limit = 50, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var logs = new List<InvocationLog>();
            
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                SELECT id, timestamp, server_name, tool_name, parameters, 
                       success, duration_ms, response_summary, error
                FROM invocations
                ORDER BY timestamp DESC
                LIMIT @limit
                """;
            cmd.Parameters.AddWithValue("@limit", limit);
            
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                logs.Add(new InvocationLog
                {
                    Id = reader.GetInt64(0),
                    Timestamp = DateTime.Parse(reader.GetString(1)),
                    ServerName = reader.GetString(2),
                    ToolName = reader.GetString(3),
                    Parameters = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Success = reader.GetInt32(5) == 1,
                    DurationMs = reader.GetInt64(6),
                    ResponseSummary = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Error = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }
            
            return logs;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<InvocationStats> GetStatsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var stats = new InvocationStats();
            
            // Overall stats
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT 
                        COUNT(*) as total,
                        SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) as success_count,
                        AVG(duration_ms) as avg_duration
                    FROM invocations
                    """;
                
                using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    stats.TotalInvocations = reader.GetInt32(0);
                    stats.SuccessCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    stats.FailureCount = stats.TotalInvocations - stats.SuccessCount;
                    stats.SuccessRate = stats.TotalInvocations > 0 
                        ? Math.Round((double)stats.SuccessCount / stats.TotalInvocations * 100, 2) 
                        : 0;
                    stats.AverageDurationMs = reader.IsDBNull(2) ? 0 : Math.Round(reader.GetDouble(2), 2);
                }
            }
            
            // Stats by tool
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT 
                        server_name, 
                        tool_name,
                        COUNT(*) as count,
                        SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) as success_count,
                        AVG(duration_ms) as avg_duration
                    FROM invocations
                    GROUP BY server_name, tool_name
                    ORDER BY count DESC
                    """;
                
                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var count = reader.GetInt32(2);
                    var successCount = reader.GetInt32(3);
                    stats.ByTool.Add(new ToolStats
                    {
                        ServerName = reader.GetString(0),
                        ToolName = reader.GetString(1),
                        Count = count,
                        SuccessCount = successCount,
                        SuccessRate = count > 0 ? Math.Round((double)successCount / count * 100, 2) : 0,
                        AverageDurationMs = Math.Round(reader.GetDouble(4), 2)
                    });
                }
            }
            
            // Stats by server
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT 
                        server_name, 
                        COUNT(*) as count,
                        SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) as success_count
                    FROM invocations
                    GROUP BY server_name
                    ORDER BY count DESC
                    """;
                
                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var count = reader.GetInt32(1);
                    var successCount = reader.GetInt32(2);
                    stats.ByServer.Add(new ServerStats
                    {
                        ServerName = reader.GetString(0),
                        Count = count,
                        SuccessCount = successCount,
                        SuccessRate = count > 0 ? Math.Round((double)successCount / count * 100, 2) : 0
                    });
                }
            }
            
            return stats;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<InvocationPattern>> GetPatternsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var patterns = new Dictionary<string, (int total, int success)>();
            
            // Get recent invocations to analyze patterns (sliding window of 3)
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                SELECT server_name, tool_name, success
                FROM invocations
                ORDER BY timestamp DESC
                LIMIT 1000
                """;
            
            var invocations = new List<(string server, string tool, bool success)>();
            using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    invocations.Add((
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetInt32(2) == 1
                    ));
                }
            }
            
            // Reverse to chronological order
            invocations.Reverse();
            
            // Find sequences of 2-3 consecutive calls
            for (int i = 0; i < invocations.Count - 1; i++)
            {
                // 2-call sequences
                var seq2 = $"{invocations[i].server}/{invocations[i].tool} → {invocations[i + 1].server}/{invocations[i + 1].tool}";
                var success2 = invocations[i].success && invocations[i + 1].success;
                
                if (!patterns.ContainsKey(seq2))
                    patterns[seq2] = (0, 0);
                
                var (total2, successCount2) = patterns[seq2];
                patterns[seq2] = (total2 + 1, successCount2 + (success2 ? 1 : 0));
                
                // 3-call sequences
                if (i < invocations.Count - 2)
                {
                    var seq3 = $"{invocations[i].server}/{invocations[i].tool} → {invocations[i + 1].server}/{invocations[i + 1].tool} → {invocations[i + 2].server}/{invocations[i + 2].tool}";
                    var success3 = invocations[i].success && invocations[i + 1].success && invocations[i + 2].success;
                    
                    if (!patterns.ContainsKey(seq3))
                        patterns[seq3] = (0, 0);
                    
                    var (total3, successCount3) = patterns[seq3];
                    patterns[seq3] = (total3 + 1, successCount3 + (success3 ? 1 : 0));
                }
            }
            
            // Return top patterns (occurring more than once)
            return patterns
                .Where(p => p.Value.total > 1)
                .OrderByDescending(p => p.Value.total)
                .Take(20)
                .Select(p => new InvocationPattern
                {
                    Sequence = p.Key,
                    Occurrences = p.Value.total,
                    SuccessCount = p.Value.success,
                    FailureCount = p.Value.total - p.Value.success,
                    SuccessRate = Math.Round((double)p.Value.success / p.Value.total * 100, 2)
                })
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        _connection.Dispose();
    }
}
