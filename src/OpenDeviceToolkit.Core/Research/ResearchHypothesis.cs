namespace OpenDeviceToolkit.Core.Research;

public sealed record ResearchHypothesis
(
    string Id,
    string Description,
    RiskLevel Risk,
    double Confidence,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string>? TestCommands = null,
    DateTime CreatedAt = default
)
{
    public ResearchHypothesis(string description, RiskLevel risk, double confidence, IReadOnlyList<string> evidence, IReadOnlyList<string>? testCommands = null)
        : this(Guid.NewGuid().ToString(), description, risk, confidence, evidence, testCommands, DateTime.UtcNow) { }
    
    public string Display => $"[{Risk.GetColor()}] {Description} (Confidence: {Confidence:P0})";
    public static ResearchHypothesis FromResult(ResearchResult result, IReadOnlyList<string> evidence) =>
        new(result.Title, result.EstimatedRisk, result.Confidence, evidence);
}
