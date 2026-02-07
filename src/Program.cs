using McpBridge.Configuration;
using McpBridge.Models;
using McpBridge.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging with --verbose support
var isVerbose = args.Contains("--verbose") || args.Contains("-v");
builder.Logging.SetMinimumLevel(isVerbose ? LogLevel.Debug : LogLevel.Information);

// Configure MCP servers settings
builder.Services.Configure<McpServersSettings>(options =>
{
    var section = builder.Configuration.GetSection(McpServersSettings.SectionName);
    options.Servers = section.Get<Dictionary<string, McpServerConfig>>() ?? new();
});

// Register services
builder.Services.AddSingleton<IMcpClientService, McpClientService>();

var app = builder.Build();

// Health check
app.MapGet("/health", (IMcpClientService mcpClient) => new HealthResponse
{
    Status = "healthy",
    Timestamp = DateTime.UtcNow,
    ActiveServers = mcpClient.GetActiveServerCount()
});

// List all configured servers
app.MapGet("/servers", (IMcpClientService mcpClient) => mcpClient.GetServerInfos());

// List tools for a specific server
app.MapGet("/servers/{name}/tools", async (string name, IMcpClientService mcpClient, CancellationToken ct) =>
{
    if (!mcpClient.GetConfiguredServers().Contains(name))
        return Results.NotFound(new { error = $"Server '{name}' not found" });

    try
    {
        var tools = await mcpClient.ListToolsAsync(name, ct);
        return Results.Ok(tools);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Invoke a tool on a server
app.MapPost("/servers/{name}/invoke", async (string name, InvokeRequest request, IMcpClientService mcpClient, CancellationToken ct) =>
{
    if (!mcpClient.GetConfiguredServers().Contains(name))
        return Results.NotFound(new { error = $"Server '{name}' not found" });

    try
    {
        var result = await mcpClient.InvokeToolAsync(name, request.Tool, request.Params, ct);
        return Results.Ok(new InvokeResponse
        {
            Success = !result.IsError,
            Result = result.Content,
            Error = result.IsError ? result.Content.FirstOrDefault()?.Text : null
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new InvokeResponse { Success = false, Error = ex.Message });
    }
});

// Shutdown a server
app.MapPost("/servers/{name}/shutdown", async (string name, IMcpClientService mcpClient) =>
{
    if (!mcpClient.GetConfiguredServers().Contains(name))
        return Results.NotFound(new { error = $"Server '{name}' not found" });

    await mcpClient.ShutdownServerAsync(name);
    return Results.Ok(new { message = $"Server '{name}' shutdown" });
});

app.Run();
