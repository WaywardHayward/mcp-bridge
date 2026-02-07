using System.Text.Json.Serialization;

namespace McpBridge.Models.Api;

/// <summary>
/// Tool invocation request.
/// </summary>
public class InvokeRequest
{
    [JsonPropertyName("tool")]
    public required string Tool { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }
}
