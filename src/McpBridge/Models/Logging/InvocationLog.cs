namespace McpBridge.Models.Logging;

/// <summary>
/// Represents a logged tool invocation.
/// </summary>
public class InvocationLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string ServerName { get; set; }
    public required string ToolName { get; set; }
    public string? Parameters { get; set; }
    public bool Success { get; set; }
    public long DurationMs { get; set; }
    public string? ResponseSummary { get; set; }
    public string? Error { get; set; }
}
