# MCP Bridge

A .NET REST API that acts as a generic bridge to any MCP (Model Context Protocol) server. It spawns and manages MCP server processes, handles JSON-RPC communication, and exposes a simple REST API for invoking MCP tools.

## Features

- **Multi-server support** - Configure and run multiple MCP servers simultaneously
- **On-demand spawning** - Servers start automatically when first accessed
- **JSON-RPC communication** - Full MCP protocol support via stdin/stdout
- **Simple REST API** - Easy-to-use HTTP endpoints for tool discovery and invocation

## Requirements

- .NET 9 SDK
- MCP server packages (e.g., via npx)

## Configuration

Configure MCP servers in `appsettings.json`:

```json
{
  "McpServers": {
    "atlassian": {
      "command": "npx",
      "args": ["-y", "@anthropics/mcp-server-atlassian"],
      "environment": {
        "ATLASSIAN_API_TOKEN": "your-token"
      }
    },
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "/path/to/dir"]
    },
    "custom": {
      "command": "/path/to/mcp-server",
      "args": ["--some-flag"],
      "workingDirectory": "/path/to/workdir"
    }
  }
}
```

### Server Configuration Options

| Property | Type | Description |
|----------|------|-------------|
| `command` | string | The command to run (required) |
| `args` | string[] | Command arguments (optional) |
| `environment` | object | Environment variables (optional) |
| `workingDirectory` | string | Working directory (optional) |

## Running

```bash
cd src
dotnet run
```

The API will be available at `http://localhost:5000`.

## API Endpoints

### Health Check

```http
GET /health
```

Returns server health status.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-02-07T11:30:00Z",
  "activeServers": 1
}
```

### List Servers

```http
GET /servers
```

Returns all configured MCP servers.

**Response:**
```json
[
  {
    "name": "atlassian",
    "command": "npx",
    "args": ["-y", "@anthropics/mcp-server-atlassian"],
    "isRunning": true
  }
]
```

### List Tools

```http
GET /servers/{name}/tools
```

Lists available tools for a specific MCP server. Starts the server if not already running.

**Response:**
```json
[
  {
    "name": "search_issues",
    "description": "Search for Jira issues",
    "inputSchema": {
      "type": "object",
      "properties": {
        "query": { "type": "string" }
      }
    }
  }
]
```

### Invoke Tool

```http
POST /servers/{name}/invoke
Content-Type: application/json

{
  "tool": "search_issues",
  "params": {
    "query": "project = DEMO"
  }
}
```

Invokes a tool on an MCP server.

**Response:**
```json
{
  "success": true,
  "result": [
    {
      "type": "text",
      "text": "Found 5 issues..."
    }
  ],
  "error": null
}
```

### Shutdown Server

```http
POST /servers/{name}/shutdown
```

Shuts down a running MCP server process.

**Response:**
```json
{
  "message": "Server 'atlassian' shutdown"
}
```

## Example Usage

```bash
# Check health
curl http://localhost:5000/health

# List configured servers
curl http://localhost:5000/servers

# List tools for a server
curl http://localhost:5000/servers/filesystem/tools

# Invoke a tool
curl -X POST http://localhost:5000/servers/filesystem/invoke \
  -H "Content-Type: application/json" \
  -d '{"tool": "read_file", "params": {"path": "/tmp/test.txt"}}'

# Shutdown a server
curl -X POST http://localhost:5000/servers/filesystem/shutdown
```

## Architecture

```
┌─────────────────┐     HTTP      ┌─────────────────┐
│   REST Client   │ ◄───────────► │   MCP Bridge    │
└─────────────────┘               └────────┬────────┘
                                           │
                                    stdin/stdout
                                   (JSON-RPC 2.0)
                                           │
                      ┌────────────────────┼────────────────────┐
                      ▼                    ▼                    ▼
              ┌───────────────┐    ┌───────────────┐    ┌───────────────┐
              │  MCP Server 1 │    │  MCP Server 2 │    │  MCP Server N │
              └───────────────┘    └───────────────┘    └───────────────┘
```

## License

MIT
