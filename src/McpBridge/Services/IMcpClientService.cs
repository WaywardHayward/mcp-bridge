using McpBridge.Models.Api;
using McpBridge.Models.Mcp;

namespace McpBridge.Services;

/// <summary>
/// Service for communicating with MCP servers.
/// </summary>
public interface IMcpClientService
{
    /// <summary>
    /// Checks if a server exists in configuration.
    /// </summary>
    bool ServerExists(string serverName);

    /// <summary>
    /// Gets information about all configured servers.
    /// </summary>
    IReadOnlyList<ServerInfo> GetServerInfos();

    /// <summary>
    /// Gets the count of active server processes.
    /// </summary>
    int GetActiveServerCount();

    /// <summary>
    /// Lists available tools for a server.
    /// </summary>
    Task<List<McpTool>> ListToolsAsync(string serverName, CancellationToken ct = default);

    /// <summary>
    /// Invokes a tool on a server and returns a clean response.
    /// </summary>
    Task<InvokeResponse> InvokeToolAsync(string serverName, InvokeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Shuts down a running server.
    /// </summary>
    Task ShutdownServerAsync(string serverName);
}
