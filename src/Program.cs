using McpBridge.Configuration;
using McpBridge.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure MCP servers settings
builder.Services.Configure<McpServersSettings>(
    builder.Configuration.GetSection(McpServersSettings.SectionName));

// Register services
builder.Services.AddSingleton<IMcpClientService, McpClientService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
