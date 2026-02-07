using System.Text.Json.Serialization;

namespace McpBridge.Models.JsonRpc;

/// <summary>
/// JSON-RPC request structure for MCP protocol.
/// </summary>
public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}
