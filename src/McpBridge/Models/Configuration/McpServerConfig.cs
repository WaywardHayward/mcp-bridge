namespace McpBridge.Models.Configuration;

/// <summary>
/// Configuration for a single MCP server.
/// </summary>
public class McpServerConfig
{
    /// <summary>
    /// Transport type - stdio (local) or sse (remote). Defaults to stdio.
    /// </summary>
    public McpTransportType Transport { get; set; } = McpTransportType.Stdio;

    // Stdio transport properties
    public string? Command { get; set; }
    public string[] Args { get; set; } = [];
    public Dictionary<string, string> Environment { get; set; } = new();
    public string? WorkingDirectory { get; set; }

    // SSE transport properties
    public string? Url { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? ApiKeyEnvVar { get; set; }
}
