# MCP Logs Implementation Plan

## Goal
Add an invocation logging system to the MCP bridge

## Architecture

### New Files
1. `Models/Logging/InvocationLog.cs` - Data model for log entries
2. `Services/Logging/IInvocationLogger.cs` - Interface
3. `Services/Logging/SqliteInvocationLogger.cs` - SQLite implementation
4. `Controllers/LogsController.cs` - REST endpoints

### Changes
1. `McpBridge.csproj` - Add SQLite package
2. `Program.cs` - Register logging service
3. `ServersController.cs` - Hook into invoke flow

## Implementation Order
1. Add SQLite NuGet package
2. Create InvocationLog model
3. Create IInvocationLogger interface
4. Create SqliteInvocationLogger implementation
5. Create LogsController with GET endpoints
6. Modify ServersController to use logger
7. Update Program.cs for DI
8. Test endpoints
9. Commit to feature branch

## Endpoints
- `GET /logs` - Recent invocations (default 50, ?limit=N)
- `GET /logs/stats` - Aggregated stats
- `GET /logs/patterns` - Common sequences

## Database Location
`~/.mcp-bridge/logs.db`
