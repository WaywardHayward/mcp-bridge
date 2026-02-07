using System.Text.Json.Serialization;

namespace McpBridge.Models;

/// <summary>
/// Server information returned by the API.
/// </summary>
public class ServerInfo
{
    public required string Name { get; set; }
    public required string Command { get; set; }
    public string[] Args { get; set; } = [];
    public bool IsRunning { get; set; }
}

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

/// <summary>
/// Tool invocation response.
/// </summary>
public class InvokeResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Health check response.
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int ActiveServers { get; set; }
}
