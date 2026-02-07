using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using McpBridge.Models.Api;
using McpBridge.Models.Logging;
using McpBridge.Services;
using McpBridge.Services.Logging;

namespace McpBridge.Controllers;

[ApiController]
[Route("servers")]
public class ServersController(IMcpClientService mcpClient, IInvocationLogger logger) : ControllerBase
{
    private static readonly HashSet<string> SensitiveParams = 
        ["password", "token", "secret", "key", "api_key", "apikey", "auth", "credential"];

    [HttpGet]
    public IActionResult List() => 
        Ok(mcpClient.GetServerInfos());

    [HttpGet("{name}/tools")]
    public async Task<IActionResult> ListTools(string name, CancellationToken ct) =>
        mcpClient.ServerExists(name) 
            ? Ok(await mcpClient.ListToolsAsync(name, ct)) 
            : NotFound(new { error = $"Server '{name}' not found" });

    [HttpPost("{name}/invoke")]
    public async Task<IActionResult> Invoke(string name, [FromBody] InvokeRequest request, CancellationToken ct)
    {
        if (!mcpClient.ServerExists(name))
            return NotFound(new { error = $"Server '{name}' not found" });

        var stopwatch = Stopwatch.StartNew();
        InvokeResponse? response = null;
        Exception? exception = null;
        
        try
        {
            response = await mcpClient.InvokeToolAsync(name, request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Log the invocation asynchronously (fire-and-forget)
            _ = LogInvocationAsync(name, request, response, exception, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task LogInvocationAsync(
        string serverName, 
        InvokeRequest request, 
        InvokeResponse? response, 
        Exception? exception,
        long durationMs)
    {
        try
        {
            var log = new InvocationLog
            {
                Timestamp = DateTime.UtcNow,
                ServerName = serverName,
                ToolName = request.Tool,
                Parameters = SanitizeParams(request.Params),
                Success = response?.Success ?? false,
                DurationMs = durationMs,
                ResponseSummary = SummarizeResponse(response),
                Error = exception?.Message ?? response?.Error
            };
            
            await logger.LogAsync(log);
        }
        catch
        {
            // Logging should never break the main flow
        }
    }

    private static string? SanitizeParams(Dictionary<string, object>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return null;
        
        var sanitized = new Dictionary<string, object>();
        foreach (var (key, value) in parameters)
        {
            if (SensitiveParams.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                sanitized[key] = "[REDACTED]";
            }
            else
            {
                sanitized[key] = value;
            }
        }
        
        var json = JsonSerializer.Serialize(sanitized);
        return json.Length > 500 ? json[..500] + "..." : json;
    }

    private static string? SummarizeResponse(InvokeResponse? response)
    {
        if (response?.Result == null)
            return null;
        
        var json = JsonSerializer.Serialize(response.Result);
        return json.Length > 200 ? json[..200] + "..." : json;
    }

    [HttpPost("{name}/shutdown")]
    public async Task<IActionResult> Shutdown(string name)
    {
        if (!mcpClient.ServerExists(name))
            return NotFound(new { error = $"Server '{name}' not found" });
        
        await mcpClient.ShutdownServerAsync(name);
        return Ok(new { message = $"Server '{name}' shutdown" });
    }
}
