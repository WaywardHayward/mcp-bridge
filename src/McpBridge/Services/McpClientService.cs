using System.Collections.Concurrent;
using McpBridge.Models.Api;
using McpBridge.Models.Configuration;
using McpBridge.Models.Mcp;
using McpBridge.Services.Transports;
using Microsoft.Extensions.Options;

namespace McpBridge.Services;

/// <summary>
/// Orchestrates MCP server communication via transports.
/// </summary>
public class McpClientService : IMcpClientService, IAsyncDisposable
{
    private readonly McpServersSettings _settings;
    private readonly IMcpTransportFactory _transportFactory;
    private readonly ConcurrentDictionary<string, IMcpTransport> _transports = new();

    public McpClientService(IOptions<McpServersSettings> settings, IMcpTransportFactory transportFactory)
    {
        _settings = settings.Value;
        _transportFactory = transportFactory;
    }

    public bool ServerExists(string serverName) => 
        _settings.Servers.ContainsKey(serverName);

    public IReadOnlyList<ServerInfo> GetServerInfos() =>
        _settings.Servers.Select(kvp => new ServerInfo
        {
            Name = kvp.Key,
            Command = kvp.Value.Command,
            Args = kvp.Value.Args,
            IsRunning = _transports.ContainsKey(kvp.Key)
        }).ToList();

    public int GetActiveServerCount() => 
        _transports.Count;

    public async Task<List<McpTool>> ListToolsAsync(string serverName, CancellationToken ct = default)
    {
        var transport = await GetOrCreateTransportAsync(serverName, ct);
        return await transport.ListToolsAsync(ct);
    }

    public async Task<InvokeResponse> InvokeToolAsync(string serverName, InvokeRequest request, CancellationToken ct = default)
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

    private async Task<InvokeResponse> InvokeToolCoreAsync(string serverName, InvokeRequest request, CancellationToken ct)
    {
        var transport = await GetOrCreateTransportAsync(serverName, ct);
        var result = await transport.CallToolAsync(request.Tool, request.Params, ct);

        return new InvokeResponse
        {
            Success = !result.IsError,
            Result = result.Content,
            Error = result.IsError ? result.Content.FirstOrDefault()?.Text : null
        };
    }

    public async Task ShutdownServerAsync(string serverName)
    {
        if (_transports.TryRemove(serverName, out var transport))
            await transport.DisposeAsync();
    }

    private async Task<IMcpTransport> GetOrCreateTransportAsync(string serverName, CancellationToken ct)
    {
        if (_transports.TryGetValue(serverName, out var existing))
            return existing;

        if (!_settings.Servers.TryGetValue(serverName, out var config))
            throw new ArgumentException($"Server '{serverName}' not found in configuration");

        var transport = _transportFactory.Create(config);
        await transport.InitializeAsync(ct);

        _transports[serverName] = transport;
        return transport;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var transport in _transports.Values)
            await transport.DisposeAsync();
        
        _transports.Clear();
    }
}
