namespace McpBridge.Models.Api;

/// <summary>
/// Tool invocation response.
/// </summary>
public class InvokeResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
}
