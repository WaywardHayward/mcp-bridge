using Microsoft.AspNetCore.Mvc;
using McpBridge.Models.Api;
using McpBridge.Services;

namespace McpBridge.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(IMcpClientService mcpClient) : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult GetHealth() => Ok(new HealthResponse
    {
        ActiveServers = mcpClient.GetActiveServerCount()
    });
}
