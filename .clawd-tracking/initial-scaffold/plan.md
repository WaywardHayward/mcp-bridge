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
- [ ] Add .gitignore
- [ ] Create GitHub repo (private)

### Phase 2: Project Structure
- [ ] Create .NET minimal API project
- [ ] Add directory structure (Models, Services, Configuration)
- [ ] Add required packages

### Phase 3: Configuration
- [ ] Create McpServerSettings class
- [ ] Update appsettings.json with example servers
- [ ] Wire up IOptions pattern

### Phase 4: MCP Client Service
- [ ] Create IMcpClientService interface
- [ ] Implement McpClientService with process management
- [ ] Handle JSON-RPC communication
- [ ] Register with DI

### Phase 5: REST Endpoints
- [ ] GET /health
- [ ] GET /servers
- [ ] GET /servers/{name}/tools
- [ ] POST /servers/{name}/invoke

### Phase 6: Documentation
- [ ] Create comprehensive README.md

### Phase 7: Review & Complete
- [ ] Review against CODE_STYLE.md
- [ ] Build verification (0 errors, 0 warnings)
- [ ] Commit and push

## Dependencies
- .NET 9 SDK (at ~/.dotnet/dotnet)
- GitHub CLI (gh)

## Notes
- MCP uses JSON-RPC over stdin/stdout
- Each MCP server is a separate process
