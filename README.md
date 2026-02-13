# MCP Bridge

A .NET REST API that acts as a generic bridge to any MCP (Model Context Protocol) server. Supports both local stdio-based servers and remote SSE-based servers.

## Features

- **Multi-server support** — Configure and run multiple MCP servers simultaneously
- **Dual transport support** — Local servers via stdio, remote servers via SSE (Server-Sent Events)
- **On-demand connection** — Servers connect automatically when first accessed
- **Invocation logging** — SQLite-based logging of all tool invocations with pattern analysis
- **JSON-RPC communication** — Full MCP protocol support
- **Simple REST API** — Easy-to-use HTTP endpoints for tool discovery and invocation

## Requirements

- .NET 9 SDK
- MCP server packages (e.g., via npx) for local servers

## Quick Start

```bash
# Clone the repo
git clone https://github.com/waywardhayward/mcp-bridge.git
cd mcp-bridge

# Copy and configure settings
cp src/McpBridge/appsettings.example.json src/McpBridge/appsettings.json
# Edit appsettings.json with your server configurations

# Run
cd src/McpBridge
dotnet run
```

The API will be available at `http://localhost:5050`.

## Configuration

Configure MCP servers in `appsettings.json`:

```json
{
  "McpServers": {
    "Servers": {
      "filesystem": {
        "Transport": "Stdio",
        "Command": "npx",
        "Args": ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"]
      },
      "atlassian": {
        "Transport": "Sse",
        "Url": "https://mcp.atlassian.com/v1/sse",
        "ApiKeyEnvVar": "ATLASSIAN_API_TOKEN"
      }
    }
  }
}
```

### Stdio Transport (Local Servers)

For locally-spawned MCP servers that communicate via stdin/stdout.

| Property | Type | Description |
|----------|------|-------------|
| `Transport` | string | Set to `"Stdio"` |
| `Command` | string | The command to run (required) |
| `Args` | string[] | Command arguments (optional) |
| `Environment` | object | Environment variables (optional) |
| `WorkingDirectory` | string | Working directory (optional) |

### SSE Transport (Remote Servers)

For remote MCP servers that use HTTP Server-Sent Events.

| Property | Type | Description |
|----------|------|-------------|
| `Transport` | string | Set to `"Sse"` |
| `Url` | string | SSE endpoint URL (required) |
| `Headers` | object | Additional HTTP headers (optional) |
| `ApiKeyEnvVar` | string | Environment variable containing API key (optional) |

## API Endpoints

### Health Check

```bash
curl http://localhost:5050/health
```

### List Servers

```bash
curl http://localhost:5050/servers
```

Returns all configured MCP servers with their running status.

### List Tools

```bash
curl http://localhost:5050/servers/filesystem/tools
```

Lists available tools for a specific MCP server.

### Invoke Tool

```bash
curl -X POST http://localhost:5050/servers/filesystem/invoke \
  -H "Content-Type: application/json" \
  -d '{"tool": "list_directory", "params": {"path": "/tmp"}}'
```

### Shutdown Server

```bash
curl -X POST http://localhost:5050/servers/filesystem/shutdown
```

Disconnects from a running server.

### Logs

```bash
# Get recent invocations
curl http://localhost:5050/logs?limit=50

# Get invocation stats
curl http://localhost:5050/logs/stats

# Get usage patterns
curl http://localhost:5050/logs/patterns
```

## Architecture

```
┌─────────────────┐     HTTP      ┌─────────────────┐
│   REST Client   │ ◄───────────► │   MCP Bridge    │
└─────────────────┘               └────────┬────────┘
                                           │
                          ┌────────────────┴────────────────┐
                          │                                 │
                    Stdio Transport                   SSE Transport
                    (stdin/stdout)                    (HTTP/SSE)
                          │                                 │
              ┌───────────┴───────────┐                     │
              ▼                       ▼                     ▼
      ┌───────────────┐       ┌───────────────┐     ┌───────────────┐
      │ Local Server  │       │ Local Server  │     │ Remote Server │
      │ (filesystem)  │       │   (sqlite)    │     │  (atlassian)  │
      └───────────────┘       └───────────────┘     └───────────────┘
```

## Project Structure

```
/
├── src/
│   └── McpBridge/              # Main application
│       ├── Controllers/        # API endpoints
│       ├── Models/             # Data models
│       │   ├── Api/            # API request/response models
│       │   ├── Configuration/
│       │   ├── JsonRpc/
│       │   ├── Logging/
│       │   └── Mcp/
│       └── Services/           # Business logic
│           ├── Logging/        # Invocation logging
│           └── Transports/     # Stdio & SSE transports
├── tests/
│   └── McpBridge.Tests/        # Unit tests
└── McpBridge.sln
```

## Running as a Service

For production deployments, you can run MCP Bridge as a systemd service:

```ini
[Unit]
Description=MCP Bridge
After=network.target

[Service]
Type=simple
WorkingDirectory=/path/to/mcp-bridge/src/McpBridge
ExecStart=/path/to/dotnet run
Restart=always
RestartSec=10

[Install]
WantedBy=default.target
```

## License

MIT
