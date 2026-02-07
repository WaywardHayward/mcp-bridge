using System.Net.Http.Json;
using System.Text.Json;
using McpBridge.Models.Configuration;
using McpBridge.Models.JsonRpc;
using McpBridge.Models.Mcp;

namespace McpBridge.Services.Transports;

/// <summary>
/// SSE-based transport for remote MCP servers.
/// </summary>
public class SseTransport : IMcpTransport
{
    private readonly McpServerConfig _config;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private int _requestId;
    private string? _sessionId;

    public SseTransport(McpServerConfig config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClient = httpClientFactory.CreateClient("MCP");
        ConfigureHttpClient();
    }

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (IsInitialized) return;

        // Connect to SSE endpoint and get session
        var sseUrl = _config.Url!;
        
        using var request = new HttpRequestMessage(HttpMethod.Get, sseUrl);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        // Read the initial SSE event to get session/endpoint info
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var endpoint = await ReadSseEndpointAsync(reader, ct);
        _sessionId = endpoint;

        // Send initialize request
        var initParams = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { },
            clientInfo = new { name = "mcp-bridge", version = "1.0.0" }
        };

        await SendRequestAsync<object>("initialize", initParams, ct);
        await SendNotificationAsync("notifications/initialized", null, ct);

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

    public ValueTask DisposeAsync()
    {
        // HttpClient is managed by factory, don't dispose
        return ValueTask.CompletedTask;
    }

    private void ConfigureHttpClient()
    {
        foreach (var header in _config.Headers)
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);

        if (!string.IsNullOrEmpty(_config.ApiKeyEnvVar))
        {
            var apiKey = Environment.GetEnvironmentVariable(_config.ApiKeyEnvVar);
            if (!string.IsNullOrEmpty(apiKey))
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    private static async Task<string> ReadSseEndpointAsync(StreamReader reader, CancellationToken ct)
    {
        // MCP SSE protocol sends an "endpoint" event first with the POST URL
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (line.StartsWith("event: endpoint"))
            {
                var dataLine = await reader.ReadLineAsync(ct);
                if (dataLine?.StartsWith("data: ") == true)
                    return dataLine[6..];
            }
        }

        throw new InvalidOperationException("Failed to get SSE endpoint from server");
    }

    private async Task<T?> SendRequestAsync<T>(string method, object? @params, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _requestId);
        var request = new JsonRpcRequest { Id = id, Method = method, Params = @params };

        var postUrl = _sessionId ?? _config.Url!;
        using var response = await _httpClient.PostAsJsonAsync(postUrl, request, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();

        // SSE response - read from stream
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        return await ReadSseResponseAsync<T>(reader, id, ct);
    }

    private async Task SendNotificationAsync(string method, object? @params, CancellationToken ct)
    {
        var notification = new { jsonrpc = "2.0", method, @params };
        var postUrl = _sessionId ?? _config.Url!;
        using var response = await _httpClient.PostAsJsonAsync(postUrl, notification, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T?> ReadSseResponseAsync<T>(StreamReader reader, int expectedId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (line.StartsWith("data: "))
            {
                var json = line[6..];
                if (json.Contains($"\"id\":{expectedId}") || json.Contains($"\"id\": {expectedId}"))
                {
                    var response = JsonSerializer.Deserialize<JsonRpcResponse<T>>(json, _jsonOptions);

                    if (response?.Error is not null)
                        throw new InvalidOperationException($"MCP error: {response.Error.Message}");

                    return response!.Result;
                }
            }
        }

        return default;
    }
}
