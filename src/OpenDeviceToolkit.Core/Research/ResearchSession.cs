using System.Text.Json;

namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Represents an active research session for a specific device or objective.
/// Tracks hypotheses, tests, results, and progress.
/// </summary>
public sealed class ResearchSession : IDisposable
{
    private readonly string _sessionId;
    private readonly string _deviceId;
    private readonly string _objective;
    private readonly List<ResearchHypothesis> _hypotheses = new();
    private readonly List<ResearchResult> _results = new();
    private readonly List<ResearchStep> _executedSteps = new();
    private DateTime _startedAt;
    private DateTime? _completedAt;
    private ResearchStatus _status = ResearchStatus.InProgress;
    
    /// <summary>
    /// Initializes a new research session.
    /// </summary>
    public ResearchSession(string deviceId, string objective)
    {
        _sessionId = Guid.NewGuid().ToString();
        _deviceId = deviceId;
        _objective = objective;
        _startedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    public string SessionId => _sessionId;
    
    /// <summary>
    /// Gets the target device identifier.
    /// </summary>
    public string DeviceId => _deviceId;
    
    /// <summary>
    /// Gets the research objective.
    /// </summary>
    public string Objective => _objective;
    
    /// <summary>
    /// Gets the session start time.
    /// </summary>
    public DateTime StartedAt => _startedAt;
    
    /// <summary>
    /// Gets the session completion time, or null if still in progress.
    /// </summary>
    public DateTime? CompletedAt => _completedAt;
    
    /// <summary>
    /// Gets the current session status.
    /// </summary>
    public ResearchStatus Status => _status;
    
    /// <summary>
    /// Gets the duration of the session.
    /// </summary>
    public TimeSpan Duration => _completedAt.HasValue 
        ? _completedAt.Value - _startedAt 
        : DateTime.UtcNow - _startedAt;
    
    /// <summary>
    /// Gets all hypotheses generated during this session.
    /// </summary>
    public IReadOnlyList<ResearchHypothesis> Hypotheses => _hypotheses.AsReadOnly();
    
    /// <summary>
    /// Gets all results collected during this session.
    /// </summary>
    public IReadOnlyList<ResearchResult> Results => _results.AsReadOnly();
    
    /// <summary>
    /// Gets all steps executed during this session.
    /// </summary>
    public IReadOnlyList<ResearchStep> ExecutedSteps => _executedSteps.AsReadOnly();
    
    /// <summary>
    /// Adds a hypothesis to the session.
    /// </summary>
    public void AddHypothesis(ResearchHypothesis hypothesis) => _hypotheses.Add(hypothesis);
    
    /// <summary>
    /// Adds a result to the session.
    /// </summary>
    public void AddResult(ResearchResult result) => _results.Add(result);
    
    /// <summary>
    /// Adds multiple results to the session.
    /// </summary>
    public void AddResults(IEnumerable<ResearchResult> results) => _results.AddRange(results);
    
    /// <summary>
    /// Records an executed step.
    /// </summary>
    public void RecordStep(ResearchStep step) => _executedSteps.Add(step);
    
    /// <summary>
    /// Gets the current best hypothesis based on confidence.
    /// </summary>
    public ResearchHypothesis? GetBestHypothesis() =>
        _hypotheses.OrderByDescending(h => h.Confidence).FirstOrDefault();
    
    /// <summary>
    /// Gets hypotheses that haven't been tested yet.
    /// </summary>
    public IReadOnlyList<ResearchHypothesis> GetUntestedHypotheses()
    {
        var testedDescriptions = _executedSteps.Select(s => s.Description).ToHashSet();
        return _hypotheses.Where(h => !testedDescriptions.Contains(h.Description)).ToList().AsReadOnly();
    }
    
    /// <summary>
    /// Completes the session with a final status.
    /// </summary>
    public void Complete(ResearchStatus status)
    {
        _status = status;
        _completedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Saves the session state to a file.
    /// </summary>
    public void Save(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"session_{_sessionId}.json");
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
    
    /// <summary>
    /// Loads a session from a file.
    /// </summary>
    public static ResearchSession? Load(string path)
    {
        if (!File.Exists(path))
            return null;
        
        try
        {
            return JsonSerializer.Deserialize<ResearchSession>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Loads all sessions from a directory.
    /// </summary>
    public static IReadOnlyList<ResearchSession> LoadAll(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<ResearchSession>();
        
        var sessions = new List<ResearchSession>();
        foreach (var file in Directory.GetFiles(directory, "session_*.json"))
        {
            var session = Load(file);
            if (session != null)
                sessions.Add(session);
        }
        return sessions;
    }
    
    public void Dispose() => Complete(ResearchStatus.Completed);
}

/// <summary>
/// Status of a research session.
/// </summary>
public enum ResearchStatus
{
    InProgress,
    Paused,
    Completed,
    Failed,
    Cancelled
}
