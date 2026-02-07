using McpBridge.Models;

namespace McpBridge.Services;

/// <summary>
/// Orchestrates MCP server communication - thin layer over process and RPC services.
/// </summary>
public class McpClientService : IMcpClientService
{
    private readonly IMcpProcessManager _processManager;
    private readonly IMcpJsonRpcClient _rpcClient;

    public McpClientService(IMcpProcessManager processManager, IMcpJsonRpcClient rpcClient)
    {
        _processManager = processManager;
        _rpcClient = rpcClient;
    }

    public bool ServerExists(string serverName) => 
        _processManager.ServerExists(serverName);

    public IReadOnlyList<ServerInfo> GetServerInfos() =>
        _processManager.GetConfiguredServers()
            .Select(name => new ServerInfo
            {
                Name = name,
                IsRunning = _processManager.IsServerRunning(name)
            }).ToList();

    public int GetActiveServerCount() => 
        _processManager.GetActiveServerCount();

    public async Task<List<McpTool>> ListToolsAsync(string serverName, CancellationToken ct = default)
    {
        var process = await GetInitializedServerAsync(serverName, ct);
        var response = await _rpcClient.SendRequestAsync<McpToolsResult>(process, "tools/list", null, ct);
        return response?.Tools ?? [];
    }

    public async Task<InvokeResponse> InvokeToolAsync(string serverName, InvokeRequest request, CancellationToken ct = default) =>
        await TryInvokeToolAsync(serverName, request, ct);

    public async Task<InvokeResponse> TryInvokeToolAsync(string serverName, InvokeRequest request, CancellationToken ct = default)
    {
        try
        {
            return await InvokeToolCoreAsync(serverName, request, ct);
        }
        catch (Exception ex)
        {
            return new InvokeResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<InvokeResponse> InvokeToolCoreAsync(string serverName, InvokeRequest request, CancellationToken ct = default)
    {
        var process = await GetInitializedServerAsync(serverName, ct);
        var callParams = new { name = request.Tool, arguments = request.Params ?? new Dictionary<string, object>() };
        var result = await _rpcClient.SendRequestAsync<McpCallToolResult>(process, "tools/call", callParams, ct);

        if (result is null)
            return new InvokeResponse { Success = false, Error = "No response from server" };

        return new InvokeResponse
        {
            Success = !result.IsError,
            Result = result.Content,
            Error = result.IsError ? result.Content.FirstOrDefault()?.Text : null
        };
    }

    public Task ShutdownServerAsync(string serverName) => 
        _processManager.ShutdownServerAsync(serverName);

    private async Task<McpServerProcess> GetInitializedServerAsync(string serverName, CancellationToken ct)
    {
        var process = await _processManager.GetOrStartServerAsync(serverName, ct);
        
        // Initialize if this is a fresh process (first call initializes it)
        // TODO: Track initialization state in process wrapper
        await _rpcClient.InitializeServerAsync(process, ct);
        
        return process;
    }
}
