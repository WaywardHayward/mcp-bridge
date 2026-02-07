using McpBridge.Models.Configuration;
using McpBridge.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure
builder.Services.Configure<McpServersSettings>(
    builder.Configuration.GetSection(McpServersSettings.SectionName));

// Register services
builder.Services.AddSingleton<IMcpProcessManager, McpProcessManager>();
builder.Services.AddSingleton<IMcpJsonRpcClient, McpJsonRpcClient>();
builder.Services.AddSingleton<IMcpClientService, McpClientService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
