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
    private const string ProtocolVersion = "2024-11-05";
    private const string ClientName = "mcp-bridge";
    private const string ClientVersion = "1.0.0";
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(30);

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

        await PerformHandshakeAsync(ct);
        
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
        return result ?? CreateNoResponseError();
    }

    public async ValueTask DisposeAsync()
    {
        await TerminateProcessAsync();
        _writeLock?.Dispose();
        _process?.Dispose();
    }

    #region Initialization

    private async Task PerformHandshakeAsync(CancellationToken ct)
    {
        var initParams = CreateInitializeParams();
        await SendRequestAsync<object>("initialize", initParams, ct);
        await SendNotificationAsync("notifications/initialized", null);
    }

    private static object CreateInitializeParams() => new
    {
        protocolVersion = ProtocolVersion,
        capabilities = new { },
        clientInfo = new { name = ClientName, version = ClientVersion }
    };

    #endregion

    #region Process Management

    private Process StartProcess()
    {
        var startInfo = CreateProcessStartInfo();
        AddEnvironmentVariables(startInfo);

        var process = new Process { StartInfo = startInfo };
        process.Start();
        return process;
    }

    private ProcessStartInfo CreateProcessStartInfo() => new()
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

    private void AddEnvironmentVariables(ProcessStartInfo startInfo)
    {
        foreach (var env in _config.Environment)
        {
            startInfo.EnvironmentVariables[env.Key] = env.Value;
        }
    }

    private async Task TerminateProcessAsync()
    {
        if (_process is null) return;
        if (_process.HasExited) return;

        _process.Kill();
        await _process.WaitForExitAsync();
    }

    #endregion

    #region JSON-RPC Communication

    private async Task<T?> SendRequestAsync<T>(string method, object? @params, CancellationToken ct)
    {
        EnsureInitialized();

        var id = Interlocked.Increment(ref _requestId);
        var request = new JsonRpcRequest { Id = id, Method = method, Params = @params };
        
        await WriteToStdinAsync(request, ct);
        
        var responseLine = await ReadResponseAsync(id, ct);
        return ParseResponse<T>(responseLine);
    }

    private async Task SendNotificationAsync(string method, object? @params)
    {
        EnsureInitialized();

        var notification = new { jsonrpc = "2.0", method };
        await WriteToStdinAsync(notification, CancellationToken.None);
    }

    private async Task WriteToStdinAsync(object message, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(message, _jsonOptions);

        await _writeLock!.WaitAsync(ct);
        try
        {
            await _process!.StandardInput.WriteLineAsync(json);
            await _process.StandardInput.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    #endregion

    #region Response Handling

    private async Task<string?> ReadResponseAsync(int expectedId, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        while (!cts.Token.IsCancellationRequested)
        {
            var line = await _process!.StandardOutput.ReadLineAsync(cts.Token);
            
            if (string.IsNullOrEmpty(line)) continue;
            if (IsResponseForId(line, expectedId)) return line;
        }

        return null;
    }

    private static bool IsResponseForId(string json, int expectedId)
    {
        return json.Contains($"\"id\":{expectedId}") || 
               json.Contains($"\"id\": {expectedId}");
    }

    private T? ParseResponse<T>(string? responseLine)
    {
        if (string.IsNullOrEmpty(responseLine)) return default;

        var response = JsonSerializer.Deserialize<JsonRpcResponse<T>>(responseLine, _jsonOptions);

        if (response?.Error is not null)
        {
            throw new InvalidOperationException($"MCP error: {response.Error.Message}");
        }

        return response!.Result;
    }

    #endregion

    #region Validation

    private void EnsureInitialized()
    {
        if (_process is null || _writeLock is null)
        {
            throw new InvalidOperationException("Transport not initialized");
        }
    }

    #endregion

    #region Error Handling

    private static McpCallToolResult CreateNoResponseError() => new()
    {
        IsError = true,
        Content = [new McpContentItem { Text = "No response" }]
    };

    #endregion
}
