using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using McpBridge.Configuration;
using McpBridge.Models;
using Microsoft.Extensions.Options;

namespace McpBridge.Services;

/// <summary>
/// Manages MCP server processes and JSON-RPC communication.
/// </summary>
public class McpClientService : IMcpClientService, IDisposable
{
    private readonly McpServersSettings _settings;
    private readonly ILogger<McpClientService> _logger;
    private readonly ConcurrentDictionary<string, McpServerProcess> _processes = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private int _requestId;
    private bool _disposed;

    public McpClientService(IOptions<McpServersSettings> settings, ILogger<McpClientService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IReadOnlyList<string> GetConfiguredServers() => _settings.Servers.Keys.ToList();

    public IReadOnlyList<ServerInfo> GetServerInfos() =>
        _settings.Servers.Select(kvp => new ServerInfo
        {
            Name = kvp.Key,
            Command = kvp.Value.Command,
            Args = kvp.Value.Args,
            IsRunning = _processes.ContainsKey(kvp.Key)
        }).ToList();

    public bool IsServerRunning(string serverName) => _processes.ContainsKey(serverName);

    public int GetActiveServerCount() => _processes.Count;

    public async Task<List<McpTool>> ListToolsAsync(string serverName, CancellationToken ct = default)
    {
        var process = await EnsureServerStartedAsync(serverName, ct);
        var response = await SendRequestAsync<McpToolsResult>(process, "tools/list", null, ct);
        return response?.Tools ?? [];
    }

    public async Task<McpCallToolResult> InvokeToolAsync(
        string serverName,
        string toolName,
        Dictionary<string, object>? parameters,
        CancellationToken ct = default)
    {
        var process = await EnsureServerStartedAsync(serverName, ct);
        var callParams = new { name = toolName, arguments = parameters ?? new Dictionary<string, object>() };
        var response = await SendRequestAsync<McpCallToolResult>(process, "tools/call", callParams, ct);
        return response ?? new McpCallToolResult { IsError = true, Content = [new McpContentItem { Text = "No response" }] };
    }

    public async Task ShutdownServerAsync(string serverName)
    {
        if (!_processes.TryRemove(serverName, out var process))
            return;

        await DisposeProcessAsync(process);
    }

    private async Task<McpServerProcess> EnsureServerStartedAsync(string serverName, CancellationToken ct)
    {
        if (_processes.TryGetValue(serverName, out var existing))
            return existing;

        if (!_settings.Servers.TryGetValue(serverName, out var config))
            throw new ArgumentException($"Server '{serverName}' not found in configuration");

        var process = await StartServerAsync(serverName, config, ct);
        _processes[serverName] = process;
        return process;
    }

    private async Task<McpServerProcess> StartServerAsync(string serverName, McpServerConfig config, CancellationToken ct)
    {
        _logger.LogInformation("Starting MCP server: {ServerName}", serverName);

        var startInfo = CreateProcessStartInfo(config);
        var process = new Process { StartInfo = startInfo };
        process.Start();

        var mcpProcess = new McpServerProcess(process);
        await InitializeServerAsync(mcpProcess, ct);

        _logger.LogInformation("MCP server {ServerName} initialized", serverName);
        return mcpProcess;
    }

    private static ProcessStartInfo CreateProcessStartInfo(McpServerConfig config) =>
        new()
        {
            FileName = config.Command,
            Arguments = string.Join(' ', config.Args),
            WorkingDirectory = config.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

    private async Task InitializeServerAsync(McpServerProcess process, CancellationToken ct)
    {
        var initParams = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { },
            clientInfo = new { name = "mcp-bridge", version = "1.0.0" }
        };

        await SendRequestAsync<object>(process, "initialize", initParams, ct);
        await SendNotificationAsync(process, "notifications/initialized", null);
    }

    private async Task<T?> SendRequestAsync<T>(McpServerProcess process, string method, object? @params, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _requestId);
        var request = new JsonRpcRequest { Id = id, Method = method, Params = @params };
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        await process.WriteLock.WaitAsync(ct);
        try
        {
            await process.Process.StandardInput.WriteLineAsync(json);
            await process.Process.StandardInput.FlushAsync();
        }
        finally
        {
            process.WriteLock.Release();
        }

        var responseLine = await ReadResponseAsync(process, id, ct);
        if (string.IsNullOrEmpty(responseLine))
            return default;

        var response = JsonSerializer.Deserialize<JsonRpcResponse<T>>(responseLine, _jsonOptions);
        if (response?.Error != null)
            throw new InvalidOperationException($"MCP error: {response.Error.Message}");

        return response!.Result;
    }

    private async Task SendNotificationAsync(McpServerProcess process, string method, object? @params)
    {
        var notification = new { jsonrpc = "2.0", method, @params };
        var json = JsonSerializer.Serialize(notification, _jsonOptions);

        await process.WriteLock.WaitAsync();
        try
        {
            await process.Process.StandardInput.WriteLineAsync(json);
            await process.Process.StandardInput.FlushAsync();
        }
        finally
        {
            process.WriteLock.Release();
        }
    }

    private static async Task<string?> ReadResponseAsync(McpServerProcess process, int expectedId, CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        while (!cts.Token.IsCancellationRequested)
        {
            var line = await process.Process.StandardOutput.ReadLineAsync(cts.Token);
            if (string.IsNullOrEmpty(line))
                continue;

            if (line.Contains($"\"id\":{expectedId}") || line.Contains($"\"id\": {expectedId}"))
                return line;
        }

        return null;
    }

    private static async Task DisposeProcessAsync(McpServerProcess process)
    {
        try
        {
            if (!process.Process.HasExited)
            {
                process.Process.Kill();
                await process.Process.WaitForExitAsync();
            }
        }
        catch { /* Ignore cleanup errors */ }
        finally
        {
            process.WriteLock.Dispose();
            process.Process.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var kvp in _processes)
        {
            DisposeProcessAsync(kvp.Value).GetAwaiter().GetResult();
        }
        _processes.Clear();
        GC.SuppressFinalize(this);
    }

    private sealed class McpServerProcess(Process process)
    {
        public Process Process { get; } = process;
        public SemaphoreSlim WriteLock { get; } = new(1, 1);
    }
}
