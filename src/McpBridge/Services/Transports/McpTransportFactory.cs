using McpBridge.Models.Configuration;

namespace McpBridge.Services.Transports;

/// <summary>
/// Factory for creating MCP transports based on configuration.
/// </summary>
public interface IMcpTransportFactory
{
    IMcpTransport Create(McpServerConfig config);
}

public class McpTransportFactory : IMcpTransportFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public McpTransportFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IMcpTransport Create(McpServerConfig config)
    {
        return config.Transport switch
        {
            McpTransportType.Stdio => CreateStdioTransport(config),
            McpTransportType.Sse => CreateSseTransport(config),
            _ => throw new ArgumentException($"Unknown transport type: {config.Transport}")
        };
    }

    private static StdioTransport CreateStdioTransport(McpServerConfig config)
    {
        if (string.IsNullOrEmpty(config.Command))
            throw new ArgumentException("Stdio transport requires Command to be set");

        return new StdioTransport(config);
    }

    private SseTransport CreateSseTransport(McpServerConfig config)
    {
        if (string.IsNullOrEmpty(config.Url))
            throw new ArgumentException("SSE transport requires Url to be set");

        return new SseTransport(config, _httpClientFactory);
    }
}
