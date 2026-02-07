using System.Text.Json.Serialization;

namespace McpBridge.Models.JsonRpc;

/// <summary>
/// JSON-RPC response structure for MCP protocol.
/// </summary>
public class JsonRpcResponse<T>
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}
