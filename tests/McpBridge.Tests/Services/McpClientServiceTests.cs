using McpBridge.Models.Api;
using McpBridge.Models.Mcp;
using McpBridge.Services;
using Moq;

namespace McpBridge.Tests.Services;

public class McpClientServiceTests
{
    private readonly Mock<IMcpProcessManager> _mockProcessManager;
    private readonly Mock<IMcpJsonRpcClient> _mockRpcClient;

    public McpClientServiceTests()
    {
        _mockProcessManager = new Mock<IMcpProcessManager>();
        _mockRpcClient = new Mock<IMcpJsonRpcClient>();
    }

    [Fact]
    public void Given_ProcessManagerHasServer_When_ServerExistsCalled_Then_ReturnsTrue()
    {
        _mockProcessManager.Setup(x => x.ServerExists("test")).Returns(true);
        var service = new McpClientService(_mockProcessManager.Object, _mockRpcClient.Object);

        var result = service.ServerExists("test");

        Assert.True(result);
    }

    [Fact]
    public void Given_ProcessManagerNoServer_When_ServerExistsCalled_Then_ReturnsFalse()
    {
        _mockProcessManager.Setup(x => x.ServerExists("unknown")).Returns(false);
        var service = new McpClientService(_mockProcessManager.Object, _mockRpcClient.Object);

        var result = service.ServerExists("unknown");

        Assert.False(result);
    }

    [Fact]
    public void Given_ConfiguredServers_When_GetServerInfosCalled_Then_ReturnsServerInfoList()
    {
        _mockProcessManager.Setup(x => x.GetConfiguredServers())
            .Returns(new List<string> { "server1", "server2" });
        _mockProcessManager.Setup(x => x.IsServerRunning("server1")).Returns(true);
        _mockProcessManager.Setup(x => x.IsServerRunning("server2")).Returns(false);
        _mockProcessManager.Setup(x => x.GetServerConfig("server1"))
            .Returns(new McpBridge.Models.Configuration.McpServerConfig { Command = "cmd1", Args = ["arg1"] });
        _mockProcessManager.Setup(x => x.GetServerConfig("server2"))
            .Returns(new McpBridge.Models.Configuration.McpServerConfig { Command = "cmd2", Args = [] });

        var service = new McpClientService(_mockProcessManager.Object, _mockRpcClient.Object);

        var result = service.GetServerInfos();

        Assert.Equal(2, result.Count);
        Assert.Equal("server1", result[0].Name);
        Assert.True(result[0].IsRunning);
        Assert.Equal("cmd1", result[0].Command);
        Assert.Equal("server2", result[1].Name);
        Assert.False(result[1].IsRunning);
    }

    [Fact]
    public void Given_ActiveServers_When_GetActiveServerCountCalled_Then_ReturnsCount()
    {
        _mockProcessManager.Setup(x => x.GetActiveServerCount()).Returns(3);
        var service = new McpClientService(_mockProcessManager.Object, _mockRpcClient.Object);

        var result = service.GetActiveServerCount();

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task Given_RpcClientThrows_When_InvokeToolAsyncCalled_Then_ReturnsErrorResponse()
    {
        var mockProcess = new Mock<McpServerProcess>(MockBehavior.Loose, new System.Diagnostics.Process());
        
        _mockProcessManager.Setup(x => x.GetOrStartServerAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockProcess.Object);
        _mockRpcClient.Setup(x => x.InitializeServerAsync(It.IsAny<McpServerProcess>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRpcClient.Setup(x => x.SendRequestAsync<McpCallToolResult>(
                It.IsAny<McpServerProcess>(), 
                "tools/call", 
                It.IsAny<object>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RPC failed"));

        var service = new McpClientService(_mockProcessManager.Object, _mockRpcClient.Object);
        var request = new InvokeRequest { Tool = "test-tool" };

        var result = await service.InvokeToolAsync("test", request);

        Assert.False(result.Success);
        Assert.Equal("RPC failed", result.Error);
    }

    [Fact]
    public async Task Given_SuccessfulRpcCall_When_InvokeToolAsyncCalled_Then_ReturnsSuccessResponse()
    {
        var mockProcess = new Mock<McpServerProcess>(MockBehavior.Loose, new System.Diagnostics.Process());
        var mcpResult = new McpCallToolResult
        {
            IsError = false,
            Content = [new McpContentItem { Type = "text", Text = "Success!" }]
        };

        _mockProcessManager.Setup(x => x.GetOrStartServerAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockProcess.Object);
        _mockRpcClient.Setup(x => x.InitializeServerAsync(It.IsAny<McpServerProcess>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRpcClient.Setup(x => x.SendRequestAsync<McpCallToolResult>(
                It.IsAny<McpServerProcess>(),
                "tools/call",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mcpResult);

        var service = new McpClientService(_mockProcessManager.Object, _mockRpcClient.Object);
        var request = new InvokeRequest { Tool = "test-tool" };

        var result = await service.InvokeToolAsync("test", request);

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Given_ServerName_When_ShutdownServerAsyncCalled_Then_DelegatesToProcessManager()
    {
        _mockProcessManager.Setup(x => x.ShutdownServerAsync("test")).Returns(Task.CompletedTask);
        var service = new McpClientService(_mockProcessManager.Object, _mockRpcClient.Object);

        await service.ShutdownServerAsync("test");

        _mockProcessManager.Verify(x => x.ShutdownServerAsync("test"), Times.Once);
    }
}
