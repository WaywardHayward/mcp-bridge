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
    private const string ProtocolVersion = "2024-11-05";
    private const string ClientName = "mcp-bridge";
    private const string ClientVersion = "1.0.0";
    private const string SseMediaType = "text/event-stream";
    private const string EndpointEventPrefix = "event: endpoint";
    private const string DataLinePrefix = "data: ";

    private readonly McpServerConfig _config;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private int _requestId;
    private string? _sessionEndpoint;

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

        _sessionEndpoint = await ConnectAndGetEndpointAsync(ct);
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

    public ValueTask DisposeAsync()
    {
        // HttpClient is managed by factory, don't dispose
        return ValueTask.CompletedTask;
    }

    #region Initialization

    private async Task<string> ConnectAndGetEndpointAsync(CancellationToken ct)
    {
        using var request = CreateSseRequest(_config.Url!);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        
        return await ReadSseEndpointAsync(reader, ct);
    }

    private async Task PerformHandshakeAsync(CancellationToken ct)
    {
        var initParams = CreateInitializeParams();
        await SendRequestAsync<object>("initialize", initParams, ct);
        await SendNotificationAsync("notifications/initialized", null, ct);
    }

    private static HttpRequestMessage CreateSseRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(SseMediaType));
        return request;
    }

    private static object CreateInitializeParams() => new
    {
        protocolVersion = ProtocolVersion,
        capabilities = new { },
        clientInfo = new { name = ClientName, version = ClientVersion }
    };

    #endregion

    #region HTTP Configuration

    private void ConfigureHttpClient()
    {
        AddCustomHeaders();
        AddBearerTokenIfConfigured();
    }

    private void AddCustomHeaders()
    {
        foreach (var header in _config.Headers)
        {
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    private void AddBearerTokenIfConfigured()
    {
        if (string.IsNullOrEmpty(_config.ApiKeyEnvVar)) return;

        var apiKey = Environment.GetEnvironmentVariable(_config.ApiKeyEnvVar);
        if (string.IsNullOrEmpty(apiKey)) return;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    #endregion

    #region SSE Protocol

    private static async Task<string> ReadSseEndpointAsync(StreamReader reader, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (!line.StartsWith(EndpointEventPrefix)) continue;

            var dataLine = await reader.ReadLineAsync(ct);
            if (dataLine?.StartsWith(DataLinePrefix) == true)
            {
                return dataLine[DataLinePrefix.Length..];
            }
        }

        throw new InvalidOperationException("Failed to get SSE endpoint from server");
    }

    private async Task<T?> ReadSseResponseAsync<T>(StreamReader reader, int expectedId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (!line.StartsWith(DataLinePrefix)) continue;

            var json = line[DataLinePrefix.Length..];
            if (!IsResponseForId(json, expectedId)) continue;

            return ParseJsonRpcResponse<T>(json);
        }

        return default;
    }

    private static bool IsResponseForId(string json, int expectedId)
    {
        // Check both compact and pretty-printed JSON formats
        return json.Contains($"\"id\":{expectedId}") || 
               json.Contains($"\"id\": {expectedId}");
    }

    private T? ParseJsonRpcResponse<T>(string json)
    {
        var response = JsonSerializer.Deserialize<JsonRpcResponse<T>>(json, _jsonOptions);

        if (response?.Error is not null)
        {
            throw new InvalidOperationException($"MCP error: {response.Error.Message}");
        }

        return response!.Result;
    }

    #endregion

    #region JSON-RPC Communication

    private async Task<T?> SendRequestAsync<T>(string method, object? @params, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _requestId);
        var request = new JsonRpcRequest { Id = id, Method = method, Params = @params };

        var postUrl = GetPostUrl();
        using var response = await _httpClient.PostAsJsonAsync(postUrl, request, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        return await ReadSseResponseAsync<T>(reader, id, ct);
    }

    private async Task SendNotificationAsync(string method, object? @params, CancellationToken ct)
    {
        var notification = new { jsonrpc = "2.0", method, @params };
        var postUrl = GetPostUrl();
        using var response = await _httpClient.PostAsJsonAsync(postUrl, notification, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
    }

    private string GetPostUrl() => _sessionEndpoint ?? _config.Url!;

    #endregion

    #region Error Handling

    private static McpCallToolResult CreateNoResponseError() => new()
    {
        IsError = true,
        Content = [new McpContentItem { Text = "No response" }]
    };

    #endregion
}
