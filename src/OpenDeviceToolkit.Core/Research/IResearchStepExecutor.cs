namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Executes one already-authorized research step against a real or simulated target.
/// Implementations own the actual hardware/tool interaction. The research engine never
/// pretends that an unimplemented operation succeeded.
/// </summary>
public interface IResearchStepExecutor
{
    Task<ResearchExecutionResult> ExecuteAsync(
        ResearchStep step,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an attempted research-step execution.
/// </summary>
public sealed record ResearchExecutionResult(
    bool Success,
    string Message,
    bool Executed,
    RiskLevel Risk);
