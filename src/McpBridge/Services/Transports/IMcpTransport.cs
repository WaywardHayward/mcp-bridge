using McpBridge.Models.Mcp;

namespace McpBridge.Services.Transports;

/// <summary>
/// Abstraction for MCP server communication transport.
/// </summary>
public interface IMcpTransport : IAsyncDisposable
{
    /// <summary>
    /// Whether this transport is connected/initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initialize the transport connection.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// List available tools.
    /// </summary>
    Task<List<McpTool>> ListToolsAsync(CancellationToken ct = default);

    /// <summary>
    /// Call a tool with parameters.
    /// </summary>
    Task<McpCallToolResult> CallToolAsync(string toolName, Dictionary<string, object>? parameters, CancellationToken ct = default);
}
