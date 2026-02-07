namespace McpBridge.Models.Logging;

/// <summary>
/// Represents a common invocation pattern/sequence.
/// </summary>
public class InvocationPattern
{
    public required string Sequence { get; set; }
    public int Occurrences { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate { get; set; }
}
