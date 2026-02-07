using Microsoft.AspNetCore.Mvc;
using McpBridge.Services.Logging;

namespace McpBridge.Controllers;

[ApiController]
[Route("logs")]
public class LogsController(IInvocationLogger logger) : ControllerBase
{
    /// <summary>
    /// Gets recent invocation logs.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);
        var logs = await logger.GetLogsAsync(limit, ct);
        return Ok(logs);
    }

    /// <summary>
    /// Gets aggregated invocation statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct = default)
    {
        var stats = await logger.GetStatsAsync(ct);
        return Ok(stats);
    }

    /// <summary>
    /// Gets common invocation patterns/sequences.
    /// </summary>
    [HttpGet("patterns")]
    public async Task<IActionResult> GetPatterns(CancellationToken ct = default)
    {
        var patterns = await logger.GetPatternsAsync(ct);
        return Ok(patterns);
    }
}
