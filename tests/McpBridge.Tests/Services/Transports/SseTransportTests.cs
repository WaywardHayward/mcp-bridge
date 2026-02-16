using System.Net;
using System.Text;
using McpBridge.Models.Configuration;
using McpBridge.Services.Transports;
using Moq;
using Moq.Protected;

namespace McpBridge.Tests.Services.Transports;

/// <summary>
/// Tests for SseTransport - focusing on what we can test without complex HTTP mocking.
/// Integration/E2E tests would cover the full SSE protocol flow.
/// </summary>
public class SseTransportTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;

    public SseTransportTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _httpClient = new HttpClient(_mockHttpHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(x => x.CreateClient("MCP")).Returns(_httpClient);
    }

    private static McpServerConfig CreateSseConfig(
        string url = "https://example.com/sse",
        Dictionary<string, string>? headers = null,
        string? apiKeyEnvVar = null) => new()
    {
        Transport = McpTransportType.Sse,
        Url = url,
        Headers = headers ?? new Dictionary<string, string>(),
        ApiKeyEnvVar = apiKeyEnvVar
    };

    #region Constructor Tests

    [Fact]
    public void Given_ValidConfig_When_Constructed_Then_IsInitializedIsFalse()
    {
        var config = CreateSseConfig();

        var transport = new SseTransport(config, _mockHttpClientFactory.Object);

        Assert.False(transport.IsInitialized);
    }

    [Fact]
    public void Given_ConfigWithHeaders_When_Constructed_Then_HeadersAreAdded()
    {
        var headers = new Dictionary<string, string> { ["X-Custom-Header"] = "test-value" };
        var config = CreateSseConfig(headers: headers);

        _ = new SseTransport(config, _mockHttpClientFactory.Object);

        Assert.True(_httpClient.DefaultRequestHeaders.Contains("X-Custom-Header"));
        Assert.Equal("test-value", _httpClient.DefaultRequestHeaders.GetValues("X-Custom-Header").First());
    }

    [Fact]
    public void Given_ConfigWithMultipleHeaders_When_Constructed_Then_AllHeadersAreAdded()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Header-1"] = "value1",
            ["X-Header-2"] = "value2"
        };
        var config = CreateSseConfig(headers: headers);

        _ = new SseTransport(config, _mockHttpClientFactory.Object);

        Assert.True(_httpClient.DefaultRequestHeaders.Contains("X-Header-1"));
        Assert.True(_httpClient.DefaultRequestHeaders.Contains("X-Header-2"));
    }

    #endregion

    #region InitializeAsync Error Tests

    [Fact]
    public async Task Given_SseServerReturnsNotFound_When_InitializeAsyncCalled_Then_ThrowsHttpRequestException()
    {
        var config = CreateSseConfig();
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));
        
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        await Assert.ThrowsAsync<HttpRequestException>(() => transport.InitializeAsync());
    }

    [Fact]
    public async Task Given_SseServerReturnsUnauthorized_When_InitializeAsyncCalled_Then_ThrowsHttpRequestException()
    {
        var config = CreateSseConfig();
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        await Assert.ThrowsAsync<HttpRequestException>(() => transport.InitializeAsync());
    }

    [Fact]
    public async Task Given_SseServerReturnsInternalError_When_InitializeAsyncCalled_Then_ThrowsHttpRequestException()
    {
        var config = CreateSseConfig();
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        await Assert.ThrowsAsync<HttpRequestException>(() => transport.InitializeAsync());
    }

    [Fact]
    public async Task Given_EmptySseResponse_When_InitializeAsyncCalled_Then_ThrowsInvalidOperationException()
    {
        var config = CreateSseConfig();
        
        // Empty response without endpoint event
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("", Encoding.UTF8, "text/event-stream")
        };
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.InitializeAsync());
    }

    [Fact]
    public async Task Given_SseResponseWithoutEndpoint_When_InitializeAsyncCalled_Then_ThrowsInvalidOperationException()
    {
        var config = CreateSseConfig();
        
        // SSE response with wrong event type
        var sseContent = "event: message\ndata: hello\n\n";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
        };
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.InitializeAsync());
    }

    [Fact]
    public async Task Given_SseResponseWithEndpointButNoData_When_InitializeAsyncCalled_Then_ThrowsInvalidOperationException()
    {
        var config = CreateSseConfig();
        
        // SSE response with endpoint event but no data line
        var sseContent = "event: endpoint\n\n";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
        };
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.InitializeAsync());
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task Given_UninitializedTransport_When_DisposeAsyncCalled_Then_CompletesSuccessfully()
    {
        var config = CreateSseConfig();
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        await transport.DisposeAsync();
        
        Assert.False(transport.IsInitialized);
    }

    [Fact]
    public async Task Given_Transport_When_DisposeAsyncCalledMultipleTimes_Then_CompletesSuccessfully()
    {
        var config = CreateSseConfig();
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        await transport.DisposeAsync();
        await transport.DisposeAsync();
        await transport.DisposeAsync();
        
        Assert.False(transport.IsInitialized);
    }

    #endregion

    #region Request Header Tests

    [Fact]
    public async Task Given_ValidSseServer_When_InitializeAsyncCalled_Then_SendsGetRequestWithSseAcceptHeader()
    {
        var config = CreateSseConfig("https://example.com/sse-endpoint");
        HttpRequestMessage? firstRequest = null;
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                // Capture only the first request
                firstRequest ??= req;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("event: endpoint\ndata: /messages\n\n", Encoding.UTF8, "text/event-stream")
            });
        
        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        try { await transport.InitializeAsync(); } catch { /* Will fail on next step, that's fine */ }
        
        Assert.NotNull(firstRequest);
        Assert.Equal(HttpMethod.Get, firstRequest.Method);
        Assert.Contains("text/event-stream", firstRequest.Headers.Accept.ToString());
    }

    #endregion

    #region Config Validation Tests

    [Fact]
    public void Given_ConfigWithEmptyUrl_When_Constructed_Then_DoesNotThrow()
    {
        // Constructor doesn't validate URL - it's validated by factory
        var config = new McpServerConfig
        {
            Transport = McpTransportType.Sse,
            Url = ""
        };

        var transport = new SseTransport(config, _mockHttpClientFactory.Object);
        
        Assert.False(transport.IsInitialized);
    }

    #endregion
}
