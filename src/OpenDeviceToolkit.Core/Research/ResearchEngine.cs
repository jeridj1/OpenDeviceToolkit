using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Core.Research;

public sealed class ResearchEngine : IDisposable
{
    private readonly List<IResearchSource> _sources = new();
    private readonly Workspace _workspace;
    private readonly AppLogger _logger;
    private readonly CommandRunner _commandRunner;
    private readonly IResearchStepExecutor _stepExecutor;
    private ResearchSession? _currentSession;
    
    public ResearchEngine(Workspace workspace, AppLogger logger, CommandRunner commandRunner, IResearchStepExecutor? stepExecutor = null)
    {
        _workspace = workspace;
        _logger = logger;
        _commandRunner = commandRunner;
        _stepExecutor = stepExecutor ?? new UnsupportedResearchStepExecutor();
        _sources.Add(new GitHubSearch(new HttpClient(), logger));
        _sources.Add(new XdaSearch(new HttpClient(), logger));
        _sources.Add(new OfflineResearcher(logger));
    }
    
    public ResearchSession? CurrentSession => _currentSession;
    public IReadOnlyList<IResearchSource> Sources => _sources.AsReadOnly();
    
    public void AddSource(IResearchSource source) => _sources.Add(source);
    public bool RemoveSource(IResearchSource source) => _sources.Remove(source);
    
    public ResearchSession StartSession(string deviceId, string objective)
    {
        _currentSession?.Dispose();
        _currentSession = new ResearchSession(deviceId, objective);
        _logger.Info($"Started research session for {deviceId}: {objective}");
        return _currentSession;
    }
    
    public void EndSession(ResearchStatus status = ResearchStatus.Completed)
    {
        if (_currentSession != null)
        {
            _currentSession.Complete(status);
            _currentSession.Save(_workspace.WorkspaceData);
            _logger.Info($"Ended session {_currentSession.SessionId}");
            _currentSession = null;
        }
    }
    
    public async Task<IReadOnlyList<ResearchResult>> SearchAsync(string query, UsbDeviceInfo? deviceInfo = null, CancellationToken ct = default)
    {
        var allResults = new List<ResearchResult>();
        foreach (var source in _sources.OrderByDescending(s => s.Priority))
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                if (await source.IsAvailableAsync(ct))
                {
                    _logger.Info($"Searching {source.Name}...");
                    allResults.AddRange(await source.SearchAsync(query, deviceInfo, ct));
                }
            }
            catch (Exception ex) { _logger.Error($"{source.Name} error: {ex.Message}", ex); }
        }
        return DeduplicateResults(allResults).OrderByDescending(r => r.Confidence).ToList().AsReadOnly();
    }
    
    private IReadOnlyList<ResearchResult> DeduplicateResults(IReadOnlyList<ResearchResult> results)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return results.Where(r => seen.Add(r.Url)).ToList().AsReadOnly();
    }
    
    public IReadOnlyList<ResearchHypothesis> GenerateHypotheses(IReadOnlyList<ResearchResult> results, UsbDeviceInfo? deviceInfo = null)
    {
        return results.Select(r => new ResearchHypothesis(r.Title, r.EstimatedRisk, r.Confidence, new List<string> { $"Source: {r.Source}", $"URL: {r.Url}" })).ToList().AsReadOnly();
    }
    
    public ResearchPlan CreatePlan(string deviceId, string objective, IReadOnlyList<ResearchHypothesis> hypotheses)
    {
        var plan = new ResearchPlan(deviceId, objective);
        foreach (var h in hypotheses.OrderByDescending(h => h.Confidence).ThenBy(h => h.Risk))
            plan.AddStep(new ResearchStep($"Test: {h.Description}", h.Risk, h.Evidence, h.Risk > RiskLevel.Reversible));
        return plan;
    }
    
    public async Task<bool> ExecuteNextStepAsync(ResearchPlan plan, bool autoConfirmSafe = true, CancellationToken ct = default)
    {
        if (plan.IsComplete) return false;

        var step = plan.NextStep;
        if (step == null) return false;

        // "Auto confirm safe" may only bypass confirmation for read-only/reversible work.
        // Persistent or destructive work always requires an explicit authorization path.
        if (step.RequiresConfirmation && (!autoConfirmSafe || step.Risk > RiskLevel.Reversible))
            return false;

        plan.Advance();
        try
        {
            _logger.Info($"Executing: {step.Description}");
            var execution = await _stepExecutor.ExecuteAsync(step, ct);
            plan.CompleteCurrentStep(execution.Success, execution.Message);
            if (!execution.Executed)
                _logger.Info($"Step not executed: {execution.Message}");
            return execution.Success;
        }
        catch (OperationCanceledException)
        {
            plan.CompleteCurrentStep(false, "Cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Step failed: {ex.Message}", ex);
            plan.CompleteCurrentStep(false, ex.Message);
            return false;
        }
    }
    
    public async Task<ResearchPlan> RunWorkflowAsync(string deviceId, string objective, UsbDeviceInfo? deviceInfo = null, bool autoExecute = false, CancellationToken ct = default)
    {
        var session = StartSession(deviceId, objective);
        var results = await SearchAsync(objective, deviceInfo, ct);
        session.AddResults(results);
        var hypotheses = GenerateHypotheses(results, deviceInfo);
        foreach (var h in hypotheses) session.AddHypothesis(h);
        var plan = CreatePlan(deviceId, objective, hypotheses);
        if (autoExecute) while (!plan.IsComplete && !ct.IsCancellationRequested) await ExecuteNextStepAsync(plan, false, ct);
        return plan;
    }
    
    public void Dispose() { EndSession(); foreach (var s in _sources.OfType<IDisposable>()) s.Dispose(); }
}
