using System.Text;
using System.Text.Json;
using OpenDeviceToolkit.Core.Research;

namespace OpenDeviceToolkit.Core.Ai;

/// <summary>
/// Prompt construction and strict response parsing for the AI router.
/// Model output is treated as untrusted: it is validated against the
/// documented schema, unknown interfaces or risk values mark an action
/// malformed instead of being trusted, and the raw response is always
/// preserved for the evidence record.
/// </summary>
public static class AiPrompts
{
    /// <summary>
    /// Valid instrument modes. Mirrors the Hardware Rp2040Mode names but kept
    /// as strings so Core has no dependency on the Hardware project.
    /// </summary>
    public static readonly IReadOnlySet<string> ValidModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "GPIO", "UART", "SPI", "I2C", "SWD", "JTAG", "CMSIS-DAP", "LogicAnalyzer", "OneWire", "CAN"
    };

    public static string BuildSystemPrompt() =>
        @"You are the embedded AI diagnostics assistant inside Open Device Toolkit (ODT), a Windows workbench for identifying, diagnosing, recovering, and repurposing electronics and e-waste that the operator owns. The operator may be non-technical.

You advise on: identifying chips from markings, UART/SPI/I2C/JTAG/SWD signal identification, voltage-safe probing, firmware and backup questions, and device recovery. Assume good faith: the operator is repairing or rescuing their own hardware.

Hard rules you must never break:
1. Safety first. Order actions safest-first. Prefer passive, read-only, high-impedance listening before any active driving of pins.
2. Never suggest voltages or currents outside a chip family's documented tolerances. If tolerances are unknown, say so and recommend measuring first.
3. You only PROPOSE actions. ODT enforces its own authorization gates and confirmation dialogs. Never imply confirmation can be skipped.
4. Respect the operator's authorized risk envelope. Never propose an action above it. If a higher-risk path is the only option, explain that in warnings instead.
5. Do not invent pinouts, datasheet values, or device facts. When evidence is insufficient, state what is unknown and how to obtain the evidence.
6. Respond with ONLY one JSON object. No markdown fences. No text outside the JSON.

Required JSON schema:
{""summary"": ""one paragraph, plain language"", ""warnings"": [""string""], ""actions"": [{""description"": ""what to do and why, in plain language"", ""mode"": ""one of: GPIO, UART, SPI, I2C, SWD, JTAG, CMSIS-DAP, LogicAnalyzer, OneWire, CAN"", ""risk"": ""one of: ReadOnly, Reversible, PersistentWrite, PotentialBrick, EWasteMode"", ""parameters"": {""name"": ""string value""}}]}

Risk definitions: ReadOnly = passive listening or measurement only. Reversible = temporarily changes state, can be undone. PersistentWrite = writes persistent device state. PotentialBrick = may permanently damage the device. EWasteMode = irreversible, authorized for disposable hardware only.

Use at most 5 actions, safest first. Every action must map to a real ODT instrument capability (the modes listed above).";

    public static string BuildUserPrompt(AiRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("OPERATOR OBJECTIVE:");
        builder.AppendLine(string.IsNullOrWhiteSpace(request.Objective) ? "(none provided)" : request.Objective.Trim());
        builder.AppendLine();
        builder.AppendLine("TARGET SUMMARY:");
        builder.AppendLine(string.IsNullOrWhiteSpace(request.TargetSummary) ? "(unidentified target)" : request.TargetSummary.Trim());
        builder.AppendLine();
        builder.AppendLine("DIAGNOSTIC EVIDENCE GATHERED SO FAR:");
        builder.AppendLine(string.IsNullOrWhiteSpace(request.DiagnosticContext) ? "(none yet)" : request.DiagnosticContext.Trim());
        builder.AppendLine();
        builder.AppendLine("AUTHORIZED RISK ENVELOPE: " + request.AuthorizedEnvelope);
        builder.AppendLine();
        builder.AppendLine("Propose next steps within the authorized risk envelope, following every rule in the system prompt. Respond with the JSON object only.");
        return builder.ToString();
    }

    /// <summary>
    /// Extracts the outermost JSON object from a response that may contain
    /// markdown fences or conversational text around the JSON.
    /// </summary>
    public static string? ExtractJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        return raw.Substring(start, end - start + 1);
    }

    public static AiRecommendation ParseRecommendation(string raw, AiProviderKind provider)
    {
        var recommendation = new AiRecommendation { RawResponse = raw, Provider = provider };
        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            recommendation.Summary = "The AI response could not be parsed as JSON; the raw response is preserved in the recommendation record.";
            recommendation.Warnings.Add("Unparseable model response. No actions were extracted.");
            return recommendation;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                recommendation.Warnings.Add("Model response was not a JSON object. No actions were extracted.");
                return recommendation;
            }

            if (root.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.String)
                recommendation.Summary = summary.GetString() ?? string.Empty;

            if (root.TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array)
                foreach (var warning in warnings.EnumerateArray())
                    if (warning.ValueKind == JsonValueKind.String)
                        recommendation.Warnings.Add(warning.GetString() ?? string.Empty);

            if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
                foreach (var element in actions.EnumerateArray())
                    ParseAction(recommendation, element);
        }
        catch (JsonException)
        {
            recommendation.Warnings.Add("Model response contained invalid JSON. No actions were extracted.");
        }
        return recommendation;
    }

    private static void ParseAction(AiRecommendation recommendation, JsonElement element)
    {
        var action = new AiProposedAction();
        if (element.ValueKind != JsonValueKind.Object)
        {
            action.Status = AiActionStatus.Malformed;
            action.StatusReason = "Action entry was not a JSON object.";
            recommendation.Actions.Add(action);
            return;
        }

        if (element.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String)
            action.Description = description.GetString() ?? string.Empty;

        var mode = element.TryGetProperty("mode", out var modeElement) && modeElement.ValueKind == JsonValueKind.String
            ? modeElement.GetString()
            : null;
        var riskText = element.TryGetProperty("risk", out var riskElement) && riskElement.ValueKind == JsonValueKind.String
            ? riskElement.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(action.Description)
            && mode is not null
            && riskText is not null
            && ValidModes.Contains(mode)
            && Enum.TryParse(riskText, ignoreCase: true, out RiskLevel risk)
            && Enum.IsDefined(risk))
        {
            action.Mode = mode;
            action.Risk = risk;
            if (element.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Object)
                foreach (var parameter in parameters.EnumerateObject())
                    action.Parameters[parameter.Name] = parameter.Value.ToString();
            recommendation.Actions.Add(action);
            return;
        }

        action.Mode = mode ?? string.Empty;
        action.Status = AiActionStatus.Malformed;
        action.StatusReason = "Action failed schema validation (missing description, unknown mode, or unknown risk value).";
        recommendation.Actions.Add(action);
    }
}
