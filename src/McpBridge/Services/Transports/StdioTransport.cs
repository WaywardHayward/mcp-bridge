using System.Diagnostics;
using System.Text.Json;
using McpBridge.Models.Configuration;
using McpBridge.Models.JsonRpc;
using McpBridge.Models.Mcp;

namespace McpBridge.Services.Transports;

/// <summary>
/// Stdio-based transport for local MCP server processes.
/// </summary>
public class StdioTransport : IMcpTransport
{
    private readonly McpServerConfig _config;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private Process? _process;
    private SemaphoreSlim? _writeLock;
    private int _requestId;

    public StdioTransport(McpServerConfig config)
    {
        _config = config;
    }

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (IsInitialized) return;

        _process = StartProcess();
        _writeLock = new SemaphoreSlim(1, 1);

        var initParams = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { },
            clientInfo = new { name = "mcp-bridge", version = "1.0.0" }
        };

        await SendRequestAsync<object>("initialize", initParams, ct);
        await SendNotificationAsync("notifications/initialized", null);

        IsInitialized = true;
    }

    public async Task<List<McpTool>> ListToolsAsync(CancellationToken ct = default)
    {
        var result = await SendRequestAsync<McpToolsResult>("tools/list", null, ct);
        return result?.Tools ?? [];
    }

    public async Task<McpCallToolResult> CallToolAsync(string toolName, Dictionary<string, object>? parameters, CancellationToken ct = default)
    {
        var callParams = new { name = toolName, arguments = parameters ?? new Dictionary<string, object>() };
        var result = await SendRequestAsync<McpCallToolResult>("tools/call", callParams, ct);
        return result ?? new McpCallToolResult { IsError = true, Content = [new McpContentItem { Text = "No response" }] };
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is not null && !_process.HasExited)
        {
            _process.Kill();
            await _process.WaitForExitAsync();
        }
        _writeLock?.Dispose();
        _process?.Dispose();
    }

    private Process StartProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _config.Command!,
            Arguments = string.Join(' ', _config.Args),
            WorkingDirectory = _config.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var env in _config.Environment)
            startInfo.EnvironmentVariables[env.Key] = env.Value;

        var process = new Process { StartInfo = startInfo };
        process.Start();
        return process;
    }

    private async Task<T?> SendRequestAsync<T>(string method, object? @params, CancellationToken ct)
    {
        if (_process is null || _writeLock is null)
            throw new InvalidOperationException("Transport not initialized");

        var id = Interlocked.Increment(ref _requestId);
        var request = new JsonRpcRequest { Id = id, Method = method, Params = @params };
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        await _writeLock.WaitAsync(ct);
        try
        {
            await _process.StandardInput.WriteLineAsync(json);
            await _process.StandardInput.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }

        var responseLine = await ReadResponseAsync(id, ct);
        return ParseResponse<T>(responseLine);
    }

    private async Task SendNotificationAsync(string method, object? @params)
    {
        if (_process is null || _writeLock is null)
            throw new InvalidOperationException("Transport not initialized");

        var notification = new { jsonrpc = "2.0", method };
        var json = JsonSerializer.Serialize(notification, _jsonOptions);

        await _writeLock.WaitAsync();
        try
        {
            await _process.StandardInput.WriteLineAsync(json);
            await _process.StandardInput.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task<string?> ReadResponseAsync(int expectedId, CancellationToken ct)
    {
        if (_process is null)
            throw new InvalidOperationException("Transport not initialized");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        while (!cts.Token.IsCancellationRequested)
        {
            var line = await _process.StandardOutput.ReadLineAsync(cts.Token);
            if (string.IsNullOrEmpty(line)) continue;
            if (line.Contains($"\"id\":{expectedId}") || line.Contains($"\"id\": {expectedId}"))
                return line;
        }

        return null;
    }

    private T? ParseResponse<T>(string? responseLine)
    {
        if (string.IsNullOrEmpty(responseLine))
            return default;

        var response = JsonSerializer.Deserialize<JsonRpcResponse<T>>(responseLine, _jsonOptions);

        if (response?.Error is not null)
            throw new InvalidOperationException($"MCP error: {response.Error.Message}");

        return response!.Result;
    }
}
