using McpBridge.Models.Api;
using McpBridge.Models.Configuration;
using McpBridge.Models.Mcp;
using McpBridge.Services;
using McpBridge.Services.Transports;
using Microsoft.Extensions.Options;
using Moq;

namespace McpBridge.Tests.Services;

public class McpClientServiceTests
{
    private readonly Mock<IOptions<McpServersSettings>> _mockOptions;
    private readonly Mock<IMcpTransportFactory> _mockTransportFactory;
    private readonly McpServersSettings _settings;

    public McpClientServiceTests()
    {
        _settings = new McpServersSettings
        {
            Servers = new Dictionary<string, McpServerConfig>
            {
                ["test-server"] = new McpServerConfig
                {
                    Transport = McpTransportType.Stdio,
                    Command = "echo",
                    Args = ["hello"]
                },
                ["remote-server"] = new McpServerConfig
                {
                    Transport = McpTransportType.Sse,
                    Url = "https://example.com/mcp"
                }
            }
        };

        _mockOptions = new Mock<IOptions<McpServersSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        _mockTransportFactory = new Mock<IMcpTransportFactory>();
    }

    [Fact]
    public void Given_ConfiguredServer_When_ServerExistsCalled_Then_ReturnsTrue()
    {
        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);

        var result = service.ServerExists("test-server");

        Assert.True(result);
    }

    [Fact]
    public void Given_UnknownServer_When_ServerExistsCalled_Then_ReturnsFalse()
    {
        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);

        var result = service.ServerExists("unknown-server");

        Assert.False(result);
    }

    [Fact]
    public void Given_ConfiguredServers_When_GetServerInfosCalled_Then_ReturnsServerInfoList()
    {
        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);

        var result = service.GetServerInfos();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Name == "test-server" && s.Command == "echo");
        Assert.Contains(result, s => s.Name == "remote-server" && s.Command == null);
    }

    [Fact]
    public void Given_NoActiveTransports_When_GetActiveServerCountCalled_Then_ReturnsZero()
    {
        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);

        var result = service.GetActiveServerCount();

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Given_ValidServer_When_ListToolsAsyncCalled_Then_ReturnsToolsFromTransport()
    {
        var mockTransport = new Mock<IMcpTransport>();
        var expectedTools = new List<McpTool>
        {
            new() { Name = "tool1", Description = "Tool 1" },
            new() { Name = "tool2", Description = "Tool 2" }
        };

        mockTransport.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTools);
        _mockTransportFactory.Setup(x => x.Create(It.IsAny<McpServerConfig>()))
            .Returns(mockTransport.Object);

        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);

        var result = await service.ListToolsAsync("test-server");

        Assert.Equal(2, result.Count);
        Assert.Equal("tool1", result[0].Name);
    }

    [Fact]
    public async Task Given_TransportThrows_When_InvokeToolAsyncCalled_Then_ReturnsErrorResponse()
    {
        var mockTransport = new Mock<IMcpTransport>();
        mockTransport.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(x => x.CallToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transport failed"));
        _mockTransportFactory.Setup(x => x.Create(It.IsAny<McpServerConfig>()))
            .Returns(mockTransport.Object);

        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);
        var request = new InvokeRequest { Tool = "test-tool" };

        var result = await service.InvokeToolAsync("test-server", request);

        Assert.False(result.Success);
        Assert.Equal("Transport failed", result.Error);
    }

    [Fact]
    public async Task Given_SuccessfulTransportCall_When_InvokeToolAsyncCalled_Then_ReturnsSuccessResponse()
    {
        var mockTransport = new Mock<IMcpTransport>();
        var mcpResult = new McpCallToolResult
        {
            IsError = false,
            Content = [new McpContentItem { Type = "text", Text = "Success!" }]
        };

        mockTransport.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(x => x.CallToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mcpResult);
        _mockTransportFactory.Setup(x => x.Create(It.IsAny<McpServerConfig>()))
            .Returns(mockTransport.Object);

        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);
        var request = new InvokeRequest { Tool = "test-tool" };

        var result = await service.InvokeToolAsync("test-server", request);

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Given_ActiveTransport_When_ShutdownServerAsyncCalled_Then_DisposesTransport()
    {
        var mockTransport = new Mock<IMcpTransport>();
        mockTransport.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<McpTool>());
        mockTransport.Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
        _mockTransportFactory.Setup(x => x.Create(It.IsAny<McpServerConfig>()))
            .Returns(mockTransport.Object);

        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);
        
        // First create the transport
        await service.ListToolsAsync("test-server");
        Assert.Equal(1, service.GetActiveServerCount());

        // Then shutdown
        await service.ShutdownServerAsync("test-server");

        Assert.Equal(0, service.GetActiveServerCount());
        mockTransport.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Given_UnknownServer_When_ListToolsAsyncCalled_Then_ThrowsArgumentException()
    {
        var service = new McpClientService(_mockOptions.Object, _mockTransportFactory.Object);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ListToolsAsync("unknown-server"));
    }
}
