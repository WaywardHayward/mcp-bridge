using Microsoft.AspNetCore.Mvc;
using McpBridge.Models;
using McpBridge.Services;

namespace McpBridge.Controllers;

[ApiController]
[Route("servers")]
public class ServersController(IMcpClientService mcpClient) : ControllerBase
{
    [HttpGet]
    public IActionResult ListServers() => Ok(mcpClient.GetServerInfos());

    [HttpGet("{name}/tools")]
    public async Task<IActionResult> ListTools(string name, CancellationToken ct)
    {
        if (!ServerExists(name))
            return NotFound(new { error = $"Server '{name}' not found" });

        try
        {
            var tools = await mcpClient.ListToolsAsync(name, ct);
            return Ok(tools);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost("{name}/invoke")]
    public async Task<IActionResult> InvokeTool(string name, [FromBody] InvokeRequest request, CancellationToken ct)
    {
        if (!ServerExists(name))
            return NotFound(new { error = $"Server '{name}' not found" });

        try
        {
            var result = await mcpClient.InvokeToolAsync(name, request.Tool, request.Params, ct);
            return Ok(CreateInvokeResponse(result));
        }
        catch (Exception ex)
        {
            return Ok(new InvokeResponse { Success = false, Error = ex.Message });
        }
    }

    [HttpPost("{name}/shutdown")]
    public async Task<IActionResult> ShutdownServer(string name)
    {
        if (!ServerExists(name))
            return NotFound(new { error = $"Server '{name}' not found" });

        await mcpClient.ShutdownServerAsync(name);
        return Ok(new { message = $"Server '{name}' shutdown" });
    }

    private bool ServerExists(string name) => 
        mcpClient.GetConfiguredServers().Contains(name);

    private static InvokeResponse CreateInvokeResponse(McpCallToolResult result) => new()
    {
        Success = !result.IsError,
        Result = result.Content,
        Error = result.IsError ? result.Content.FirstOrDefault()?.Text : null
    };
}
