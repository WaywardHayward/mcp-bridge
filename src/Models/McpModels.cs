using System.Text.Json.Serialization;

namespace McpBridge.Models;

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

/// <summary>
/// JSON-RPC error structure.
/// </summary>
public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// MCP tool definition.
/// </summary>
public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("inputSchema")]
    public object? InputSchema { get; set; }
}

/// <summary>
/// MCP tools list result.
/// </summary>
public class McpToolsResult
{
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = [];
}

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
