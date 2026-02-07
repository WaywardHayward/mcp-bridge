using System.Collections.Concurrent;
using System.Diagnostics;
using McpBridge.Models.Configuration;
using Microsoft.Extensions.Options;

namespace McpBridge.Services;

public class McpProcessManager : IMcpProcessManager, IDisposable
{
    private readonly McpServersSettings _settings;
    private readonly ILogger<McpProcessManager> _logger;
    private readonly ConcurrentDictionary<string, McpServerProcess> _processes = new();
    private bool _disposed;

    public McpProcessManager(IOptions<McpServersSettings> settings, ILogger<McpProcessManager> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool ServerExists(string serverName) => _settings.Servers.ContainsKey(serverName);

    public bool IsServerRunning(string serverName) => _processes.ContainsKey(serverName);

    public int GetActiveServerCount() => _processes.Count;

    public IReadOnlyList<string> GetConfiguredServers() => _settings.Servers.Keys.ToList();

    public McpServerConfig? GetServerConfig(string serverName) =>
        _settings.Servers.TryGetValue(serverName, out var config) ? config : null;

    public Task<McpServerProcess> GetOrStartServerAsync(string serverName, CancellationToken ct = default)
    {
        if (_processes.TryGetValue(serverName, out var existing))
            return Task.FromResult(existing);

        if (!_settings.Servers.TryGetValue(serverName, out var config))
            throw new ArgumentException($"Server '{serverName}' not found in configuration");

        var process = StartProcess(serverName, config);
        _processes[serverName] = process;
        return Task.FromResult(process);
    }

    public async Task ShutdownServerAsync(string serverName)
    {
        if (!_processes.TryRemove(serverName, out var process))
            return;

        await KillProcessAsync(process);
    }

    private McpServerProcess StartProcess(string serverName, McpServerConfig config)
    {
        _logger.LogInformation("Starting MCP server: {ServerName}", serverName);

        var startInfo = new ProcessStartInfo
        {
            FileName = config.Command,
            Arguments = string.Join(' ', config.Args),
            WorkingDirectory = config.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        _logger.LogInformation("MCP server {ServerName} started (PID: {Pid})", serverName, process.Id);
        return new McpServerProcess(process);
    }

    private static async Task KillProcessAsync(McpServerProcess process)
    {
        try
        {
            if (!process.Process.HasExited)
            {
                process.Process.Kill();
                await process.Process.WaitForExitAsync();
            }
        }
        catch { /* Ignore cleanup errors */ }
        finally
        {
            process.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var process in _processes.Values)
            KillProcessAsync(process).GetAwaiter().GetResult();
        
        _processes.Clear();
        GC.SuppressFinalize(this);
    }
}
