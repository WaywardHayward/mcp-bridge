using System.Text.Json;
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

public class McpJsonRpcClient : IMcpJsonRpcClient
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private int _requestId;

    public async Task InitializeServerAsync(McpServerProcess process, CancellationToken ct = default)
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

    public async Task<T?> SendRequestAsync<T>(McpServerProcess process, string method, object? @params, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _requestId);
        var request = new JsonRpcRequest { Id = id, Method = method, Params = @params };
        
        await WriteMessageAsync(process, request, ct);
        var responseLine = await ReadResponseAsync(process, id, ct);
        
        return ParseResponse<T>(responseLine);
    }

    private async Task SendNotificationAsync(McpServerProcess process, string method, object? @params)
    {
        var notification = new { jsonrpc = "2.0", method, @params };
        await WriteMessageAsync(process, notification, CancellationToken.None);
    }

    private async Task WriteMessageAsync(McpServerProcess process, object message, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(message, _jsonOptions);

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
    }

    private static async Task<string?> ReadResponseAsync(McpServerProcess process, int expectedId, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        while (!cts.Token.IsCancellationRequested)
        {
            var line = await process.Process.StandardOutput.ReadLineAsync(cts.Token);
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
        
        if (response?.Error != null)
            throw new InvalidOperationException($"MCP error: {response.Error.Message}");

        return response!.Result;
    }
}
