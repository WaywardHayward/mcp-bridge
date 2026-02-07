using Microsoft.AspNetCore.Mvc;
using McpBridge.Services;

namespace McpBridge.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(IMcpClientService mcpClient) : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult GetHealth() => Ok(new
    {
        Status = "healthy",
        Timestamp = DateTime.UtcNow,
        ActiveServers = mcpClient.GetActiveServerCount()
    });
}
