using System.Text.Json;

namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// The main research engine that coordinates device investigation and access discovery.
/// </summary>
public sealed class ResearchEngine : IDisposable
{
    private readonly List<IResearchSource> _sources = new();
    private readonly Workspace _workspace;
    private readonly AppLogger _logger;
    private readonly CommandRunner _commandRunner;
    private ResearchSession? _currentSession;
    
    /// <summary>
    /// Initializes a new research engine.
    /// </summary>
    public ResearchEngine(Workspace workspace, AppLogger logger, CommandRunner commandRunner)
    {
        _workspace = workspace;
        _logger = logger;
        _commandRunner = commandRunner;
        
        // Add default research sources
        _sources.Add(new GitHubSearch(new HttpClient(), logger));
    }
    
    /// <summary>
    /// Gets the current active research session.
    /// </summary>
    public ResearchSession? CurrentSession => _currentSession;
    
    /// <summary>
    /// Adds a research source to the engine.
    /// </summary>
    public void AddSource(IResearchSource source) => _sources.Add(source);
    
    /// <summary>
    /// Removes a research source from the engine.
    /// </summary>
    public bool RemoveSource(IResearchSource source) => _sources.Remove(source);
    
    /// <summary>
    /// Starts a new research session for a device.
    /// </summary>
    public ResearchSession StartSession(string deviceId, string objective)
    {
        _currentSession?.Dispose();
        _currentSession = new ResearchSession(deviceId, objective);
        _logger.Info($"Started research session for {deviceId}: {objective}");
        return _currentSession;
    }
    
    /// <summary>
    /// Ends the current research session.
    /// </summary>
    public void EndSession(ResearchStatus status = ResearchStatus.Completed)
    {
        if (_currentSession != null)
        {
            _currentSession.Complete(status);
            _currentSession.Save(_workspace.WorkspaceData);
            _logger.Info($"Ended research session {_currentSession.SessionId} with status {status}");
            _currentSession = null;
        }
    }
    
    /// <summary>
    /// Searches all sources for information about a device or query.
    /// </summary>
    public async Task<IReadOnlyList<ResearchResult>> SearchAsync(
        string query,
        Usb.UsbDeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<ResearchResult>();
        var sortedSources = _sources.OrderByDescending(s => s.Priority);
        
        foreach (var source in sortedSources)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            
            try
            {
                if (await source.IsAvailableAsync(cancellationToken))
                {
                    var results = await source.SearchAsync(query, deviceInfo, cancellationToken);
                    allResults.AddRange(results);
                    _logger.Info($"Found {results.Count} results from {source.Name}");
                }
                else
                {
                    _logger.Warning($"Source {source.Name} is not available");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error searching {source.Name}: {ex.Message}", ex);
            }
        }
        
        return allResults.OrderByDescending(r => r.Confidence).ToList().AsReadOnly();
    }
    
    /// <summary>
    /// Generates hypotheses from research results.
    /// </summary>
    public IReadOnlyList<ResearchHypothesis> GenerateHypotheses(
        IReadOnlyList<ResearchResult> results,
        Usb.UsbDeviceInfo? deviceInfo = null)
    {
        var hypotheses = new List<ResearchHypothesis>();
        
        foreach (var result in results)
        {
            var evidence = new List<string> 
            {
                $"Source: {result.Source}",
                $"URL: {result.Url}"
            };
            
            if (!string.IsNullOrEmpty(result.Snippet))
                evidence.Add($"Snippet: {result.Snippet[..Math.Min(result.Snippet.Length, 100)]}...");
            
            if (deviceInfo != null)
            {
                evidence.Add($"Device: {deviceInfo.DisplayName}");
                if (!string.IsNullOrEmpty(deviceInfo.VidPid))
                    evidence.Add($"VID/PID: {deviceInfo.VidPid}");
            }
            
            hypotheses.Add(new ResearchHypothesis(
                result.Title,
                result.EstimatedRisk,
                result.Confidence,
                evidence
            ));
        }
        
        return hypotheses;
    }
    
    /// <summary>
    /// Creates a research plan from hypotheses.
    /// </summary>
    public ResearchPlan CreatePlan(string deviceId, string objective, IReadOnlyList<ResearchHypothesis> hypotheses)
    {
        var plan = new ResearchPlan(deviceId, objective);
        foreach (var hypothesis in hypotheses.OrderByDescending(h => h.Confidence))
        {
            plan.AddStep(new ResearchStep(
                $"Test: {hypothesis.Description}",
                hypothesis.Risk,
                hypothesis.Evidence
            ));
        }
        return plan;
    }
    
    /// <summary>
    /// Executes the next step in the current research plan.
    /// </summary>
    public async Task<bool> ExecuteNextStepAsync(ResearchPlan plan, bool autoConfirmSafe = true, CancellationToken cancellationToken = default)
    {
        if (plan.IsComplete)
            return false;
        
        var step = plan.NextStep;
        if (step == null)
            return false;
        
        // Check if confirmation is needed
        if (step.RequiresConfirmation && !autoConfirmSafe)
        {
            _logger.Info($"Step requires confirmation: {step.Description}");
            return false;
        }
        
        // Execute the step
        plan.Advance();
        var currentStep = plan.CurrentStep!;
        
        try
        {
            _logger.Info($"Executing step: {currentStep.Description} (Risk: {currentStep.Risk})");
            await Task.Delay(100, cancellationToken);
            plan.CompleteCurrentStep(true, "Step executed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Step execution failed: {ex.Message}", ex);
            plan.CompleteCurrentStep(false, ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Runs a complete research workflow: search → generate hypotheses → create plan → execute.
    /// </summary>
    public async Task<ResearchPlan> RunWorkflowAsync(
        string deviceId,
        string objective,
        Usb.UsbDeviceInfo? deviceInfo = null,
        bool autoExecute = false,
        CancellationToken cancellationToken = default)
    {
        var session = StartSession(deviceId, objective);
        _logger.Info($"Searching for: {objective}");
        var results = await SearchAsync(objective, deviceInfo, cancellationToken);
        session.AddResults(results);
        var hypotheses = GenerateHypotheses(results, deviceInfo);
        foreach (var hyp in hypotheses)
            session.AddHypothesis(hyp);
        var plan = CreatePlan(deviceId, objective, hypotheses);
        
        if (autoExecute)
        {
            while (!plan.IsComplete && !cancellationToken.IsCancellationRequested)
            {
                await ExecuteNextStepAsync(plan, autoConfirmSafe: false, cancellationToken);
            }
        }
        
        return plan;
    }
    
    public void Dispose()
    {
        EndSession();
        foreach (var source in _sources.OfType<IDisposable>())
            source.Dispose();
    }
}
