using System.Diagnostics;

namespace McpBridge.Services;

/// <summary>
/// Wrapper for an MCP server process with thread-safe write access.
/// </summary>
public sealed class McpServerProcess(Process process) : IDisposable
{
    public Process Process { get; } = process;
    public SemaphoreSlim WriteLock { get; } = new(1, 1);
    public bool IsInitialized { get; set; }

    public void Dispose()
    {
        WriteLock.Dispose();
        Process.Dispose();
    }
}
