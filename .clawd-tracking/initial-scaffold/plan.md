# MCP Bridge Scaffold

## Objective
Create a .NET REST API that acts as a generic bridge to any MCP (Model Context Protocol) server, allowing clients to spawn MCP servers, list tools, and invoke them via HTTP.

## Success Criteria
- [ ] .NET minimal API project builds with 0 errors, 0 warnings
- [ ] Configuration supports multiple MCP server definitions
- [ ] MCP client service spawns and manages MCP server processes
- [ ] REST endpoints expose server list, tools, and invocation
- [ ] README documents usage and configuration
- [ ] Code follows CODE_STYLE.md guidelines
- [ ] All files under 200 lines

## Phases

### Phase 1: Setup
- [x] Create ~/dev/mcp-bridge directory
- [x] git init, create main branch
- [x] Create .clawd-tracking/initial-scaffold/plan.md
- [x] Add .gitignore
- [x] Create GitHub repo (private)

### Phase 2: Project Structure
- [x] Create .NET minimal API project
- [x] Add directory structure (Models, Services, Configuration)
- [x] Add required packages

### Phase 3: Configuration
- [x] Create McpServerSettings class
- [x] Update appsettings.json with example servers
- [x] Wire up IOptions pattern

### Phase 4: MCP Client Service
- [x] Create IMcpClientService interface
- [x] Implement McpClientService with process management
- [x] Handle JSON-RPC communication
- [x] Register with DI

### Phase 5: REST Endpoints
- [x] GET /health
- [x] GET /servers
- [x] GET /servers/{name}/tools
- [x] POST /servers/{name}/invoke

### Phase 6: Documentation
- [x] Create comprehensive README.md

### Phase 7: Review & Complete
- [x] Review against CODE_STYLE.md
- [x] Build verification (0 errors, 0 warnings)
- [x] Commit and push

## Dependencies
- .NET 9 SDK (at ~/.dotnet/dotnet)
- GitHub CLI (gh)

## Notes
- MCP uses JSON-RPC over stdin/stdout
- Each MCP server is a separate process
