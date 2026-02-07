using System.Text.Json.Serialization;

namespace McpBridge.Models.Mcp;

/// <summary>
/// MCP tool call result.
/// </summary>
public class McpCallToolResult
{
    [JsonPropertyName("content")]
    public List<McpContentItem> Content { get; set; } = [];

    [JsonPropertyName("isError")]
    public bool IsError { get; set; }
}
