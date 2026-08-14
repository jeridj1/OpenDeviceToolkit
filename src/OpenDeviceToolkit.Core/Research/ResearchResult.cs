namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Represents a single result from a research query or operation.
/// </summary>
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
    /// <summary>
    /// Gets a user-friendly display string for this result.
    /// </summary>
    public string Display => $"[{EstimatedRisk.GetColor()}] {Title} ({Source}) - Confidence: {Confidence:P0}";
    
    /// <summary>
    /// Creates a new research result with a generated ID.
    /// </summary>
    public static ResearchResult Create(
        string title,
        string source,
        string url,
        string? snippet = null,
        double confidence = 0.5,
        RiskLevel estimatedRisk = RiskLevel.ReadOnly,
        IReadOnlyList<string>? tags = null)
    {
        return new ResearchResult(
            Id: Guid.NewGuid().ToString(),
            Title: title,
            Source: source,
            Url: url,
            Snippet: snippet,
            Confidence: Math.Clamp(confidence, 0.0, 1.0),
            EstimatedRisk: estimatedRisk,
            Timestamp: DateTime.UtcNow,
            Tags: tags
        );
    }
}
