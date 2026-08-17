namespace OpenDeviceToolkit.Hardware;

public sealed record ProbeElectricalState(double? TargetVoltage, double? MaximumSafeVoltage, bool VoltageKnown, bool DriveAllowed, string Reason);
public sealed record ProbeExecutionPlan(ProbeModeProfile Mode, IReadOnlyList<Rp2040PinAssignment> Pins, ProbeElectricalState ElectricalState, IReadOnlyList<string> Actions);

/// <summary>Converts a selected probe mode into a staged plan. Unknown electrical conditions stay non-driving.</summary>
public sealed class ProbeExecutionPlanner
{
    public ProbeExecutionPlan Prepare(ProbeModeProfile profile, IEnumerable<Rp2040PinAssignment> pins, double? targetVoltage)
    {
        var voltageKnown = targetVoltage is > 0 and <= 5.5;
        var safeForRp2040 = targetVoltage is >= 1.8 and <= 3.6;
        var driveAllowed = profile.CanDriveTarget && safeForRp2040;
        var state = new ProbeElectricalState(targetVoltage, 3.6, voltageKnown, driveAllowed,
            !voltageKnown ? "Target voltage is unknown; all target-facing GPIO remains input-only." :
            !safeForRp2040 ? "Target voltage is outside the RP2040 GPIO domain; use appropriate level shifting." :
            "Target voltage is within the expected RP2040 GPIO domain.");
        var actions = new List<string>
        {
            "Set every target-facing GPIO to high impedance.",
            "Measure/confirm target voltage before enabling outputs.",
            driveAllowed ? "Configure only the requested output pins." : "Keep all target-facing GPIO in input/high-impedance mode.",
            "Begin with the least invasive protocol transaction available."
        };
        return new(profile, pins.ToArray(), state, actions);
    }
}
