using McpBridge.Models.Configuration;
using McpBridge.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace McpBridge.Tests.Services;

public class McpProcessManagerTests
{
    private readonly Mock<IOptions<McpServersSettings>> _mockOptions;
    private readonly Mock<ILogger<McpProcessManager>> _mockLogger;
    private readonly McpServersSettings _settings;

    public McpProcessManagerTests()
    {
        _settings = new McpServersSettings
        {
            Servers = new Dictionary<string, McpServerConfig>
            {
                ["test-server"] = new McpServerConfig
                {
                    Command = "echo",
                    Args = ["hello"]
                },
                ["another-server"] = new McpServerConfig
                {
                    Command = "cat",
                    Args = []
                }
            }
        };

        _mockOptions = new Mock<IOptions<McpServersSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        _mockLogger = new Mock<ILogger<McpProcessManager>>();
    }

    [Fact]
    public void Given_ConfiguredServer_When_ServerExistsCalled_Then_ReturnsTrue()
    {
        var manager = new McpProcessManager(_mockOptions.Object, _mockLogger.Object);

        var result = manager.ServerExists("test-server");

        Assert.True(result);
    }

    [Fact]
    public void Given_UnknownServer_When_ServerExistsCalled_Then_ReturnsFalse()
    {
        var manager = new McpProcessManager(_mockOptions.Object, _mockLogger.Object);

        var result = manager.ServerExists("unknown-server");

        Assert.False(result);
    }

    [Fact]
    public void Given_ConfiguredServers_When_GetConfiguredServersCalled_Then_ReturnsAllServerNames()
    {
        var manager = new McpProcessManager(_mockOptions.Object, _mockLogger.Object);

        var result = manager.GetConfiguredServers();

        Assert.Equal(2, result.Count);
        Assert.Contains("test-server", result);
        Assert.Contains("another-server", result);
    }

    [Fact]
    public void Given_NoRunningServers_When_GetActiveServerCountCalled_Then_ReturnsZero()
    {
        var manager = new McpProcessManager(_mockOptions.Object, _mockLogger.Object);

        var result = manager.GetActiveServerCount();

        Assert.Equal(0, result);
    }

    [Fact]
    public void Given_NoRunningServers_When_IsServerRunningCalled_Then_ReturnsFalse()
    {
        var manager = new McpProcessManager(_mockOptions.Object, _mockLogger.Object);

        var result = manager.IsServerRunning("test-server");

        Assert.False(result);
    }

    [Fact]
    public void Given_ConfiguredServer_When_GetServerConfigCalled_Then_ReturnsConfig()
    {
        var manager = new McpProcessManager(_mockOptions.Object, _mockLogger.Object);

        var result = manager.GetServerConfig("test-server");

        Assert.NotNull(result);
        Assert.Equal("echo", result.Command);
        Assert.Single(result.Args);
        Assert.Equal("hello", result.Args[0]);
    }

    [Fact]
    public void Given_UnknownServer_When_GetServerConfigCalled_Then_ReturnsNull()
    {
        var manager = new McpProcessManager(_mockOptions.Object, _mockLogger.Object);

        var result = manager.GetServerConfig("unknown-server");

        Assert.Null(result);
    }

    [Fact]
    public async Task Given_UnknownServer_When_GetOrStartServerAsyncCalled_Then_ThrowsArgumentException()
    {
        var manager = new McpProcessManager(_mockOptions.Object, _mockLogger.Object);

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.GetOrStartServerAsync("unknown-server"));
    }
}
