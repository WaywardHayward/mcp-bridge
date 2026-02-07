namespace McpBridge.Models;

/// <summary>
/// Server information returned by the API.
/// </summary>
public class ServerInfo
{
    public required string Name { get; set; }
    public string? Command { get; set; }
    public string[] Args { get; set; } = [];
    public bool IsRunning { get; set; }
}
