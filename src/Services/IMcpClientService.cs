using McpBridge.Models;

namespace McpBridge.Services;

/// <summary>
/// Service for communicating with MCP servers.
/// </summary>
public interface IMcpClientService
{
    /// <summary>
    /// Gets the list of configured server names.
    /// </summary>
    IReadOnlyList<string> GetConfiguredServers();

    /// <summary>
    /// Gets information about all configured servers.
    /// </summary>
    IReadOnlyList<ServerInfo> GetServerInfos();

    /// <summary>
    /// Checks if a server is currently running.
    /// </summary>
    bool IsServerRunning(string serverName);

    /// <summary>
    /// Lists available tools for a server.
    /// </summary>
    Task<List<McpTool>> ListToolsAsync(string serverName, CancellationToken ct = default);

    /// <summary>
    /// Invokes a tool on a server.
    /// </summary>
    Task<McpCallToolResult> InvokeToolAsync(
        string serverName, 
        string toolName, 
        Dictionary<string, object>? parameters, 
        CancellationToken ct = default);

    /// <summary>
    /// Shuts down a running server.
    /// </summary>
    Task ShutdownServerAsync(string serverName);

    /// <summary>
    /// Gets the count of active server processes.
    /// </summary>
    int GetActiveServerCount();
}
