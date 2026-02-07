using System.Text.Json.Serialization;

namespace McpBridge.Models.Mcp;

/// <summary>
/// MCP tool call result content.
/// </summary>
public class McpContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
