namespace OpenDeviceToolkit.Hardware;

public enum TargetSessionStage { Safe, VoltageChecked, Captured, Analyzed, Identified, Connected }
public sealed record TargetSessionState(TargetSessionStage Stage, double? TargetVoltage, IReadOnlyList<ProtocolHypothesis> Hypotheses, string Summary);

public sealed class TargetSessionController
{
    private readonly ProbeExecutionPlanner _planner = new();
    private readonly ProtocolDetector _detector = new();
    public TargetSessionState Begin(ProbeModeProfile profile, IEnumerable<Rp2040PinAssignment> pins, double? targetVoltage)
    {
        var plan = _planner.Prepare(profile, pins, targetVoltage);
        return new(TargetSessionStage.VoltageChecked, targetVoltage, [], plan.ElectricalState.Reason);
    }
    public TargetSessionState Analyze(TargetSessionState state, LogicCapture capture, int bit = 0)
    {
        var hypotheses = _detector.Analyze(capture, bit);
        var summary = hypotheses.Count == 0 ? "No strong protocol hypothesis." : $"Top hypothesis: {hypotheses[0].Protocol} ({hypotheses[0].Confidence:P0}).";
        return state with { Stage = TargetSessionStage.Analyzed, Hypotheses = hypotheses, Summary = summary };
    }
}
