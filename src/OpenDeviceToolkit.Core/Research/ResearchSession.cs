using System.Text.Json;

namespace OpenDeviceToolkit.Core.Research;

public sealed class ResearchSession : IDisposable
{
    private readonly string _sessionId;
    private readonly string _deviceId;
    private readonly string _objective;
    private readonly List<ResearchHypothesis> _hypotheses = new();
    private readonly List<ResearchResult> _results = new();
    private DateTime _startedAt;
    private DateTime? _completedAt;
    private ResearchStatus _status = ResearchStatus.InProgress;
    
    public ResearchSession(string deviceId, string objective)
    {
        _sessionId = Guid.NewGuid().ToString();
        _deviceId = deviceId;
        _objective = objective;
        _startedAt = DateTime.UtcNow;
    }
    
    public string SessionId => _sessionId;
    public string DeviceId => _deviceId;
    public string Objective => _objective;
    public DateTime StartedAt => _startedAt;
    public DateTime? CompletedAt => _completedAt;
    public ResearchStatus Status => _status;
    public TimeSpan Duration => _completedAt.HasValue ? _completedAt.Value - _startedAt : DateTime.UtcNow - _startedAt;
    public IReadOnlyList<ResearchHypothesis> Hypotheses => _hypotheses.AsReadOnly();
    public IReadOnlyList<ResearchResult> Results => _results.AsReadOnly();
    
    public void AddHypothesis(ResearchHypothesis h) => _hypotheses.Add(h);
    public void AddResult(ResearchResult r) => _results.Add(r);
    public ResearchHypothesis? GetBestHypothesis() => _hypotheses.OrderByDescending(h => h.Confidence).FirstOrDefault();
    public void Complete(ResearchStatus status) { _status = status; _completedAt = DateTime.UtcNow; }
    
    public void Save(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"session_{_sessionId}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    public static ResearchSession? Load(string path)
    {
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<ResearchSession>(File.ReadAllText(path));
    }
    
    public void Dispose() => Complete(ResearchStatus.Completed);
}

public enum ResearchStatus
{
    InProgress, Paused, Completed, Failed, Cancelled
}
