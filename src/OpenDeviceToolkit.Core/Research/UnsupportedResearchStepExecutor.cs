namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Safe default executor. It refuses to claim that a research step ran when no
/// concrete hardware/tool adapter has been registered.
/// </summary>
public sealed class UnsupportedResearchStepExecutor : IResearchStepExecutor
{
    public Task<ResearchExecutionResult> ExecuteAsync(
        ResearchStep step,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResearchExecutionResult(
            Success: false,
            Message: "No execution adapter is registered for this research step.",
            Executed: false,
            Risk: step.Risk));
    }
}
