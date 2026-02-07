using Microsoft.AspNetCore.Mvc;
using McpBridge.Models;
using McpBridge.Services;

namespace McpBridge.Controllers;

[ApiController]
[Route("servers")]
public class ServersController(IMcpClientService mcpClient) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => 
        Ok(mcpClient.GetServerInfos());

    [HttpGet("{name}/tools")]
    public async Task<IActionResult> ListTools(string name, CancellationToken ct) =>
        mcpClient.ServerExists(name) 
            ? Ok(await mcpClient.ListToolsAsync(name, ct)) 
            : NotFound(new { error = $"Server '{name}' not found" });

    [HttpPost("{name}/invoke")]
    public async Task<IActionResult> Invoke(string name, [FromBody] InvokeRequest request, CancellationToken ct) =>
        mcpClient.ServerExists(name) 
            ? Ok(await mcpClient.InvokeToolAsync(name, request, ct)) 
            : NotFound(new { error = $"Server '{name}' not found" });

    [HttpPost("{name}/shutdown")]
    public async Task<IActionResult> Shutdown(string name)
    {
        if (!mcpClient.ServerExists(name))
            return NotFound(new { error = $"Server '{name}' not found" });
        
        await mcpClient.ShutdownServerAsync(name);
        return Ok(new { message = $"Server '{name}' shutdown" });
    }
}
