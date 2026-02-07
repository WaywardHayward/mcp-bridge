using McpBridge.Models.Configuration;
using McpBridge.Services.Transports;
using Moq;

namespace McpBridge.Tests.Services.Transports;

public class McpTransportFactoryTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

    public McpTransportFactoryTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(x => x.CreateClient("MCP"))
            .Returns(new HttpClient());
    }

    [Fact]
    public void Given_StdioConfig_When_CreateCalled_Then_ReturnsStdioTransport()
    {
        var config = new McpServerConfig
        {
            Transport = McpTransportType.Stdio,
            Command = "echo",
            Args = ["hello"]
        };
        var factory = new McpTransportFactory(_mockHttpClientFactory.Object);

        var result = factory.Create(config);

        Assert.IsType<StdioTransport>(result);
    }

    [Fact]
    public void Given_SseConfig_When_CreateCalled_Then_ReturnsSseTransport()
    {
        var config = new McpServerConfig
        {
            Transport = McpTransportType.Sse,
            Url = "https://example.com/mcp"
        };
        var factory = new McpTransportFactory(_mockHttpClientFactory.Object);

        var result = factory.Create(config);

        Assert.IsType<SseTransport>(result);
    }

    [Fact]
    public void Given_StdioConfigWithoutCommand_When_CreateCalled_Then_ThrowsArgumentException()
    {
        var config = new McpServerConfig
        {
            Transport = McpTransportType.Stdio,
            Command = null
        };
        var factory = new McpTransportFactory(_mockHttpClientFactory.Object);

        Assert.Throws<ArgumentException>(() => factory.Create(config));
    }

    [Fact]
    public void Given_SseConfigWithoutUrl_When_CreateCalled_Then_ThrowsArgumentException()
    {
        var config = new McpServerConfig
        {
            Transport = McpTransportType.Sse,
            Url = null
        };
        var factory = new McpTransportFactory(_mockHttpClientFactory.Object);

        Assert.Throws<ArgumentException>(() => factory.Create(config));
    }
}
