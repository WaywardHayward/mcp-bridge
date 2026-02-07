namespace McpBridge.Models.Configuration;

/// <summary>
/// Transport type for MCP server communication.
/// </summary>
public enum McpTransportType
{
    /// <summary>
    /// Local process via stdin/stdout.
    /// </summary>
    Stdio,

    /// <summary>
    /// Remote server via HTTP SSE.
    /// </summary>
    Sse
}
