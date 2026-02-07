using McpBridge.Models.Configuration;

namespace McpBridge.Services;

/// <summary>
/// Manages MCP server process lifecycle.
/// </summary>
public interface IMcpProcessManager
{
    bool ServerExists(string serverName);
    bool IsServerRunning(string serverName);
    int GetActiveServerCount();
    IReadOnlyList<string> GetConfiguredServers();
    McpServerConfig? GetServerConfig(string serverName);
    Task<McpServerProcess> GetOrStartServerAsync(string serverName, CancellationToken ct = default);
    Task ShutdownServerAsync(string serverName);
}
