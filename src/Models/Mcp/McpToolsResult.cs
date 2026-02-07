using System.Text.Json.Serialization;

namespace McpBridge.Models.Mcp;

/// <summary>
/// MCP tools list result.
/// </summary>
public class McpToolsResult
{
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = [];
}
