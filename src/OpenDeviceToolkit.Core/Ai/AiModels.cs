using OpenDeviceToolkit.Core.Research;

namespace OpenDeviceToolkit.Core.Ai;

/// <summary>
/// Providers the AI router can route through. Cloud providers use official
/// bring-your-own-key endpoints; Ollama runs locally with no key at all.
/// </summary>
public enum AiProviderKind
{
    None = 0,
    Mistral,
    OpenAi,
    OpenRouter,
    Ollama
}

/// <summary>
/// Validation status of a single AI-proposed action.
/// </summary>
public enum AiActionStatus
{
    /// <summary>Well-formed and inside the operator's authorized risk envelope.</summary>
    Proposed,
    /// <summary>Well-formed but above the authorized risk envelope; visible, never queued.</summary>
    BlockedByEnvelope,
    /// <summary>The model returned an action that failed schema validation.</summary>
    Malformed
}

/// <summary>
/// A diagnostic request for the AI router: what the operator wants and the
/// evidence gathered so far.
/// </summary>
public sealed class AiRequest
{
    public string Objective { get; set; } = string.Empty;
    public string TargetSummary { get; set; } = string.Empty;
    public string DiagnosticContext { get; set; } = string.Empty;
    public RiskLevel AuthorizedEnvelope { get; set; } = RiskLevel.ReadOnly;
}

/// <summary>
/// One proposed instrument operation returned by the model, after strict
/// validation. Mode is kept as a string so Core does not depend on the
/// Hardware project; the App layer maps it to Rp2040Mode.
/// </summary>
public sealed class AiProposedAction
{
    public string Description { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public RiskLevel Risk { get; set; } = RiskLevel.ReadOnly;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public AiActionStatus Status { get; set; } = AiActionStatus.Proposed;
    public string StatusReason { get; set; } = string.Empty;
}

/// <summary>
/// A parsed, validated AI recommendation.
/// </summary>
public sealed class AiRecommendation
{
    public string Summary { get; set; } = string.Empty;
    public List<AiProposedAction> Actions { get; } = new();
    public List<string> Warnings { get; } = new();
    public string RawResponse { get; set; } = string.Empty;
    public AiProviderKind Provider { get; set; }
}

/// <summary>
/// Outcome of a router run.
/// </summary>
public enum AiRouterStatus
{
    Ok,
    InvalidRequest,
    NoProviderConfigured,
    AllProvidersFailed
}

public sealed class AiRouterResult
{
    public AiRouterStatus Status { get; init; }
    public AiRecommendation? Recommendation { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> ProviderErrors { get; init; } = Array.Empty<string>();
}
