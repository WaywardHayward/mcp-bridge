# MCP Bridge

A .NET REST API that acts as a generic bridge to any MCP (Model Context Protocol) server. Supports both local stdio-based servers and remote SSE-based servers.

## Features

- **Multi-server support** - Configure and run multiple MCP servers simultaneously
- **Dual transport support** - Local servers via stdio, remote servers via SSE (Server-Sent Events)
- **On-demand connection** - Servers connect automatically when first accessed
- **JSON-RPC communication** - Full MCP protocol support
- **Simple REST API** - Easy-to-use HTTP endpoints for tool discovery and invocation

## Requirements

- .NET 9 SDK
- MCP server packages (e.g., via npx) for local servers

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

## Running

```bash
cd src/McpBridge
dotnet run
```

The API will be available at `http://localhost:5000`.

## API Endpoints

### Health Check

```http
GET /health
```

Returns server health status.

### List Servers

```http
GET /servers
```

Returns all configured MCP servers with their running status.

### List Tools

```http
GET /servers/{name}/tools
```

Lists available tools for a specific MCP server.

### Invoke Tool

```http
POST /servers/{name}/invoke
Content-Type: application/json

{
  "tool": "tool_name",
  "params": { ... }
}
```

### Shutdown Server

```http
POST /servers/{name}/shutdown
```

Shuts down/disconnects from a server.

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
│   └── McpBridge/           # Main application
│       ├── Controllers/     # API endpoints
│       ├── Models/          # Data models
│       │   ├── Api/         # API request/response models
│       │   ├── Configuration/
│       │   ├── JsonRpc/
│       │   └── Mcp/
│       └── Services/        # Business logic
│           └── Transports/  # Stdio & SSE transports
├── tests/
│   └── McpBridge.Tests/     # Unit tests
└── McpBridge.sln
```

## License

MIT
