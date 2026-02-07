namespace McpBridge.Configuration;

/// <summary>
/// Configuration for all MCP servers.
/// </summary>
public class McpServersSettings
{
    public const string SectionName = "McpServers";

    public Dictionary<string, McpServerConfig> Servers { get; set; } = new();
}
