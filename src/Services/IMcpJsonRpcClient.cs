using McpBridge.Models;

namespace McpBridge.Services;

/// <summary>
/// Handles JSON-RPC communication with MCP servers.
/// </summary>
public interface IMcpJsonRpcClient
{
    Task InitializeServerAsync(McpServerProcess process, CancellationToken ct = default);
    Task<T?> SendRequestAsync<T>(McpServerProcess process, string method, object? @params, CancellationToken ct = default);
}
