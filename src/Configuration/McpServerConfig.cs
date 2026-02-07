namespace McpBridge.Configuration;

/// <summary>
/// Configuration for a single MCP server.
/// </summary>
public class McpServerConfig
{
    public required string Command { get; set; }
    public string[] Args { get; set; } = [];
    public Dictionary<string, string> Environment { get; set; } = new();
    public string? WorkingDirectory { get; set; }
}
