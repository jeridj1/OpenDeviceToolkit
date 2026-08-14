namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Represents a plan for researching and achieving a specific objective.
/// </summary>
public sealed class ResearchPlan
{
    private readonly List<ResearchStep> _steps = new();
    private int _currentStepIndex = -1;
    
    /// <summary>
    /// Gets the objective this plan aims to achieve.
    /// </summary>
    public string Objective { get; }
    
    /// <summary>
    /// Gets the target device identifier.
    /// </summary>
    public string DeviceId { get; }
    
    /// <summary>
    /// Gets all steps in this plan.
    /// </summary>
    public IReadOnlyList<ResearchStep> Steps => _steps.AsReadOnly();
    
    /// <summary>
    /// Gets the current step index.
    /// </summary>
    public int CurrentStepIndex => _currentStepIndex;
    
    /// <summary>
    /// Gets whether all steps have been completed.
    /// </summary>
    public bool IsComplete => _currentStepIndex >= _steps.Count - 1;
    
    /// <summary>
    /// Initializes a new research plan.
    /// </summary>
    public ResearchPlan(string deviceId, string objective)
    {
        DeviceId = deviceId;
        Objective = objective;
    }
    
    /// <summary>
    /// Adds a step to the plan.
    /// </summary>
    public void AddStep(ResearchStep step) => _steps.Add(step);
    
    /// <summary>
    /// Gets the current step.
    /// </summary>
    public ResearchStep? CurrentStep => _currentStepIndex >= 0 && _currentStepIndex < _steps.Count 
        ? _steps[_currentStepIndex] 
        : null;
    
    /// <summary>
    /// Gets the next step without advancing.
    /// </summary>
    public ResearchStep? NextStep => _currentStepIndex + 1 < _steps.Count 
        ? _steps[_currentStepIndex + 1] 
        : null;
    
    /// <summary>
    /// Advances to the next step.
    /// </summary>
    public bool Advance()
    {
        if (_currentStepIndex + 1 < _steps.Count)
        {
            _currentStepIndex++;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Marks the current step as completed and advances.
    /// </summary>
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
    
    /// <summary>
    /// Creates a research plan from a session's best hypothesis.
    /// </summary>
    public static ResearchPlan FromHypothesis(ResearchSession session, ResearchHypothesis hypothesis)
    {
        var plan = new ResearchPlan(session.DeviceId, session.Objective);
        
        // Add steps based on the hypothesis
        if (hypothesis.Risk <= RiskLevel.Reversible)
        {
            // Safe steps can be executed directly
            plan.AddStep(new ResearchStep(
                $"Test hypothesis: {hypothesis.Description}",
                hypothesis.Risk,
                hypothesis.Evidence
            ));
        }
        else
        {
            // Higher risk steps require confirmation
            plan.AddStep(new ResearchStep(
                $"CONFIRM: Execute high-risk hypothesis: {hypothesis.Description}",
                hypothesis.Risk,
                hypothesis.Evidence,
                requiresConfirmation: true
            ));
        }
        
        return plan;
    }
}

/// <summary>
/// Represents a single step in a research plan.
/// </summary>
public sealed class ResearchStep
{
    /// <summary>
    /// Gets or sets the step description.
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the risk level of this step.
    /// </summary>
    public RiskLevel Risk { get; set; }
    
    /// <summary>
    /// Gets or sets the evidence supporting this step.
    /// </summary>
    public IReadOnlyList<string> Evidence { get; set; } = new List<string>();
    
    /// <summary>
    /// Gets or sets whether this step requires explicit user confirmation.
    /// </summary>
    public bool RequiresConfirmation { get; set; }
    
    /// <summary>
    /// Gets or sets whether this step has been completed.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Gets or sets the result of this step.
    /// </summary>
    public string? Result { get; set; }
    
    /// <summary>
    /// Gets or sets when this step was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Initializes a new research step.
    /// </summary>
    public ResearchStep(string description, RiskLevel risk, IReadOnlyList<string>? evidence = null, bool requiresConfirmation = false)
    {
        Description = description;
        Risk = risk;
        Evidence = evidence ?? new List<string>();
        RequiresConfirmation = requiresConfirmation || risk.RequiresConfirmation();
    }
    
    /// <summary>
    /// Gets whether this step is ready to execute.
    /// </summary>
    public bool Ready => !RequiresConfirmation;
}
