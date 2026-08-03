namespace OpenDeviceToolkit.Hardware;

public enum ProbeModeProfile { PassiveSniffer, Uart, Spi, I2c, Jtag, Swd }
public sealed record Rp2040PinAssignment(string Signal, int Gpio);
public sealed record ElectricalState(bool Safe, double? TargetVoltage, string Reason);
public sealed record ProbeExecutionPlan(ElectricalState ElectricalState, IReadOnlyList<Rp2040PinAssignment> Pins, ProbeModeProfile Mode);

public sealed class ProbeExecutionPlanner
{
    public ProbeExecutionPlan Prepare(ProbeModeProfile mode, IEnumerable<Rp2040PinAssignment> pins, double? targetVoltage)
    {
        var assignments = pins.ToArray();
        var unique = assignments.Select(x => x.Gpio).Distinct().Count() == assignments.Length;
        var valid = assignments.All(x => x.Gpio is >= 0 and <= 29);
        var safeVoltage = targetVoltage is null || targetVoltage <= 3.3;
        var safe = unique && valid && safeVoltage;
        var reason = safe ? "Pin map passes basic RP2040 electrical checks; target remains passive until explicitly configured." : "Probe configuration rejected: check duplicate/invalid GPIO assignments and target voltage before connecting.";
        return new(new ElectricalState(safe, targetVoltage, reason), assignments, mode);
    }
}
