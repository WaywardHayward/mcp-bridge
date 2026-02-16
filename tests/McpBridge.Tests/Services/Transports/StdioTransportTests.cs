using System.Diagnostics;
using McpBridge.Models.Configuration;
using McpBridge.Services.Transports;

namespace McpBridge.Tests.Services.Transports;

/// <summary>
/// Tests for StdioTransport - focusing on process lifecycle and configuration.
/// Some tests are limited to what we can test without spawning real processes.
/// </summary>
public class StdioTransportTests
{
    private static McpServerConfig CreateStdioConfig(
        string command = "echo",
        string[]? args = null,
        Dictionary<string, string>? environment = null,
        string? workingDirectory = null) => new()
    {
        Transport = McpTransportType.Stdio,
        Command = command,
        Args = args ?? [],
        Environment = environment ?? new Dictionary<string, string>(),
        WorkingDirectory = workingDirectory
    };

    #region Constructor Tests

    [Fact]
    public void Given_ValidConfig_When_Constructed_Then_IsInitializedIsFalse()
    {
        var config = CreateStdioConfig();

        var transport = new StdioTransport(config);

        Assert.False(transport.IsInitialized);
    }

    [Fact]
    public void Given_ConfigWithArgs_When_Constructed_Then_DoesNotThrow()
    {
        var config = CreateStdioConfig(args: ["--version", "--verbose"]);

        var transport = new StdioTransport(config);

        Assert.False(transport.IsInitialized);
    }

    [Fact]
    public void Given_ConfigWithEnvironment_When_Constructed_Then_DoesNotThrow()
    {
        var config = CreateStdioConfig(environment: new Dictionary<string, string>
        {
            ["MY_VAR"] = "my_value",
            ["ANOTHER_VAR"] = "another_value"
        });

        var transport = new StdioTransport(config);

        Assert.False(transport.IsInitialized);
    }

    [Fact]
    public void Given_ConfigWithWorkingDirectory_When_Constructed_Then_DoesNotThrow()
    {
        var config = CreateStdioConfig(workingDirectory: "/tmp");

        var transport = new StdioTransport(config);

        Assert.False(transport.IsInitialized);
    }

    #endregion

    #region InitializeAsync Error Tests

    [Fact]
    public async Task Given_NonExistentCommand_When_InitializeAsyncCalled_Then_ThrowsException()
    {
        var config = CreateStdioConfig(command: "/non/existent/command/that/does/not/exist");

        var transport = new StdioTransport(config);

        // Should throw when trying to start the process
        await Assert.ThrowsAnyAsync<Exception>(() => transport.InitializeAsync());
    }

    [Fact]
    public async Task Given_InvalidWorkingDirectory_When_InitializeAsyncCalled_Then_ThrowsException()
    {
        var config = CreateStdioConfig(
            command: "echo",
            workingDirectory: "/non/existent/directory/path"
        );

        var transport = new StdioTransport(config);

        // Should throw when trying to start with invalid working directory
        await Assert.ThrowsAnyAsync<Exception>(() => transport.InitializeAsync());
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task Given_UninitializedTransport_When_DisposeAsyncCalled_Then_CompletesSuccessfully()
    {
        var config = CreateStdioConfig();
        var transport = new StdioTransport(config);

        await transport.DisposeAsync();

        Assert.False(transport.IsInitialized);
    }

    [Fact]
    public async Task Given_Transport_When_DisposeAsyncCalledMultipleTimes_Then_CompletesSuccessfully()
    {
        var config = CreateStdioConfig();
        var transport = new StdioTransport(config);

        await transport.DisposeAsync();
        await transport.DisposeAsync();
        await transport.DisposeAsync();

        Assert.False(transport.IsInitialized);
    }

    #endregion

    #region ListToolsAsync Without Init Tests

    [Fact]
    public async Task Given_UninitializedTransport_When_ListToolsAsyncCalled_Then_ThrowsInvalidOperationException()
    {
        var config = CreateStdioConfig();
        var transport = new StdioTransport(config);

        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.ListToolsAsync());
    }

    #endregion

    #region CallToolAsync Without Init Tests

    [Fact]
    public async Task Given_UninitializedTransport_When_CallToolAsyncCalled_Then_ThrowsInvalidOperationException()
    {
        var config = CreateStdioConfig();
        var transport = new StdioTransport(config);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.CallToolAsync("test-tool", new Dictionary<string, object> { ["param"] = "value" }));
    }

    [Fact]
    public async Task Given_UninitializedTransport_When_CallToolAsyncWithNullParams_Then_ThrowsInvalidOperationException()
    {
        var config = CreateStdioConfig();
        var transport = new StdioTransport(config);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.CallToolAsync("test-tool", null));
    }

    #endregion

    #region Config Edge Cases

    [Fact]
    public void Given_EmptyArgs_When_Constructed_Then_DoesNotThrow()
    {
        var config = CreateStdioConfig(args: []);

        var transport = new StdioTransport(config);

        Assert.False(transport.IsInitialized);
    }

    [Fact]
    public void Given_EmptyEnvironment_When_Constructed_Then_DoesNotThrow()
    {
        var config = CreateStdioConfig(environment: new Dictionary<string, string>());

        var transport = new StdioTransport(config);

        Assert.False(transport.IsInitialized);
    }

    [Fact]
    public void Given_NullWorkingDirectory_When_Constructed_Then_DoesNotThrow()
    {
        var config = CreateStdioConfig(workingDirectory: null);

        var transport = new StdioTransport(config);

        Assert.False(transport.IsInitialized);
    }

    #endregion
}
