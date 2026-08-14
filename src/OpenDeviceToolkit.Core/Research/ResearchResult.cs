namespace OpenDeviceToolkit.Core.Research;

public sealed record ResearchResult
(
    string Id,
    string Title,
    string Source,
    string Url,
    string? Snippet,
    double Confidence,
    RiskLevel EstimatedRisk,
    DateTime Timestamp,
    IReadOnlyList<string>? Tags = null
)
{
    public string Display => $"[{EstimatedRisk.GetColor()}] {Title} ({Source}) - Confidence: {Confidence:P0}";
    public static ResearchResult Create(string title, string source, string url, string? snippet = null, double confidence = 0.5, RiskLevel risk = RiskLevel.ReadOnly, IReadOnlyList<string>? tags = null) =>
        new(Guid.NewGuid().ToString(), title, source, url, snippet, Math.Clamp(confidence, 0.0, 1.0), risk, DateTime.UtcNow, tags);
}
