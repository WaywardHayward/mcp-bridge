using McpBridge.Models.Configuration;
using McpBridge.Services;
using McpBridge.Services.Logging;
using McpBridge.Services.Transports;

var builder = WebApplication.CreateBuilder(args);

// Configure
builder.Services.Configure<McpServersSettings>(
    builder.Configuration.GetSection(McpServersSettings.SectionName));

// HTTP client factory for SSE transports
builder.Services.AddHttpClient("MCP");

// Register services
builder.Services.AddSingleton<IMcpTransportFactory, McpTransportFactory>();
builder.Services.AddSingleton<IMcpClientService, McpClientService>();
builder.Services.AddSingleton<IInvocationLogger, SqliteInvocationLogger>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
