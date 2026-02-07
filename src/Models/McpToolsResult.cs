using System.Text.Json.Serialization;

namespace McpBridge.Models;

/// <summary>
/// MCP tools list result.
/// </summary>
public class McpToolsResult
{
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = [];
}
