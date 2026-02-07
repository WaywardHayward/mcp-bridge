using McpBridge.Configuration;
using McpBridge.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging with --verbose support
var isVerbose = args.Contains("--verbose") || args.Contains("-v");
builder.Logging.SetMinimumLevel(isVerbose ? LogLevel.Debug : LogLevel.Information);

// Configure MCP servers settings - clean bind, inject anywhere
builder.Services.Configure<McpServersSettings>(
    builder.Configuration.GetSection(McpServersSettings.SectionName));

// Register services
builder.Services.AddSingleton<IMcpClientService, McpClientService>();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
