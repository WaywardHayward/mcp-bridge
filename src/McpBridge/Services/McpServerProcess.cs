using System.Diagnostics;

namespace McpBridge.Services;

/// <summary>
/// Wrapper for an MCP server process with thread-safe write access.
/// </summary>
public class McpServerProcess : IDisposable
{
    public McpServerProcess(Process process)
    {
        Process = process;
    }

    public virtual Process Process { get; }
    public virtual SemaphoreSlim WriteLock { get; } = new(1, 1);
    public virtual bool IsInitialized { get; set; }

    public virtual void Dispose()
    {
        WriteLock.Dispose();
        Process.Dispose();
    }
}
