namespace McpBridge.Models.Logging;

/// <summary>
/// Aggregated statistics for tool invocations.
/// </summary>
public class InvocationStats
{
    public int TotalInvocations { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDurationMs { get; set; }
    public List<ToolStats> ByTool { get; set; } = [];
    public List<ServerStats> ByServer { get; set; } = [];
}

public class ToolStats
{
    public required string ServerName { get; set; }
    public required string ToolName { get; set; }
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDurationMs { get; set; }
}

public class ServerStats
{
    public required string ServerName { get; set; }
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public double SuccessRate { get; set; }
}
