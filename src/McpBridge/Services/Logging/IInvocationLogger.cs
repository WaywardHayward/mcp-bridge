using McpBridge.Models.Logging;

namespace McpBridge.Services.Logging;

/// <summary>
/// Service for logging and querying tool invocations.
/// </summary>
public interface IInvocationLogger
{
    /// <summary>
    /// Logs a tool invocation.
    /// </summary>
    Task LogAsync(InvocationLog log, CancellationToken ct = default);

    /// <summary>
    /// Gets recent invocation logs.
    /// </summary>
    Task<List<InvocationLog>> GetLogsAsync(int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets aggregated invocation statistics.
    /// </summary>
    Task<InvocationStats> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets common invocation patterns/sequences.
    /// </summary>
    Task<List<InvocationPattern>> GetPatternsAsync(CancellationToken ct = default);
}
