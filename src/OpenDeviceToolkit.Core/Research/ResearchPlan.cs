namespace OpenDeviceToolkit.Core.Research;

public sealed class ResearchPlan
{
    private readonly List<ResearchStep> _steps = new();
    private int _currentStepIndex = -1;
    
    public string Objective { get; }
    public string DeviceId { get; }
    public IReadOnlyList<ResearchStep> Steps => _steps.AsReadOnly();
    public int CurrentStepIndex => _currentStepIndex;
    public bool IsComplete => _currentStepIndex >= _steps.Count - 1;
    
    public ResearchPlan(string deviceId, string objective)
    {
        DeviceId = deviceId;
        Objective = objective;
    }
    
    public void AddStep(ResearchStep step) => _steps.Add(step);
    public ResearchStep? CurrentStep => _currentStepIndex >= 0 && _currentStepIndex < _steps.Count ? _steps[_currentStepIndex] : null;
    public ResearchStep? NextStep => _currentStepIndex + 1 < _steps.Count ? _steps[_currentStepIndex + 1] : null;
    
    public bool Advance() => _currentStepIndex + 1 < _steps.Count && ++_currentStepIndex >= 0;
    
    public bool CompleteCurrentStep(bool success, string? result = null)
    {
        if (CurrentStep != null)
        {
            CurrentStep.CompletedAt = DateTime.UtcNow;
            CurrentStep.Success = success;
            CurrentStep.Result = result;
            return Advance();
        }
        return false;
    }
    
    public static ResearchPlan FromHypothesis(ResearchSession session, ResearchHypothesis hypothesis)
    {
        var plan = new ResearchPlan(session.DeviceId, session.Objective);
        plan.AddStep(new ResearchStep(
            hypothesis.Risk <= RiskLevel.Reversible ? $"Test: {hypothesis.Description}" : $"CONFIRM: {hypothesis.Description}",
            hypothesis.Risk,
            hypothesis.Evidence,
            hypothesis.Risk > RiskLevel.Reversible
        ));
        return plan;
    }
}

public sealed class ResearchStep
{
    public string Description { get; set; }
    public RiskLevel Risk { get; set; }
    public IReadOnlyList<string> Evidence { get; set; } = new List<string>();
    public bool RequiresConfirmation { get; set; }
    public bool Success { get; set; }
    public string? Result { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Ready => !RequiresConfirmation;
    
    public ResearchStep(string description, RiskLevel risk, IReadOnlyList<string>? evidence = null, bool requiresConfirmation = false)
    {
        Description = description;
        Risk = risk;
        Evidence = evidence ?? new List<string>();
        RequiresConfirmation = requiresConfirmation || risk.RequiresConfirmation();
    }
}
