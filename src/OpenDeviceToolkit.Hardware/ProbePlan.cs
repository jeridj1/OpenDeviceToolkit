namespace OpenDeviceToolkit.Hardware;

public enum ProbeTransport { Auto, Serial, StLink, CmsisDap, PicoBridge }
public enum ProbeRisk { ReadOnly, RequiresConfirmation }
public sealed record ProbeTarget(string Description, string? PartHint = null, ProbeTransport Transport = ProbeTransport.Auto);
public sealed record ProbeConnection(string Signal, string Pin, string Purpose);
public sealed record ProbePlan(ProbeTransport Transport, IReadOnlyList<ProbeConnection> Connections, IReadOnlyList<string> Steps, IReadOnlyList<string> SafetyNotes, ProbeRisk Risk);

public sealed class ProbePlanner
{
    public ProbePlan Build(ProbeTarget target)
    {
        var transport = target.Transport != ProbeTransport.Auto ? target.Transport : InferTransport(target.Description);
        return transport switch
        {
            ProbeTransport.StLink or ProbeTransport.CmsisDap => BuildDebugPlan(transport),
            ProbeTransport.Serial => BuildSerialPlan(),
            _ => BuildPicoPlan()
        };
    }

    private static ProbeTransport InferTransport(string description)
    {
        var d = description.ToLowerInvariant();
        if (d.Contains("st-link") || d.Contains("stlink") || d.Contains("swd")) return ProbeTransport.StLink;
        if (d.Contains("uart") || d.Contains("serial") || d.Contains("console")) return ProbeTransport.Serial;
        return ProbeTransport.PicoBridge;
    }

    private static ProbePlan BuildDebugPlan(ProbeTransport transport) => new(transport,
        [new("GND", "GND", "Common reference"), new("VTref", "Target Vref", "Target voltage sense"), new("SWDIO", "SWDIO", "Debug data"), new("SWCLK", "SWCLK", "Debug clock"), new("NRST", "Reset", "Optional reset control")],
        ["Confirm target voltage before connecting signal pins.", "Detect the probe and enumerate its capabilities.", "Read the target IDCODE using a non-destructive debug transaction.", "Identify the likely MCU family from the returned ID.", "Only after identification, offer family-specific read-only probes."],
        ["Do not power an unknown target from the probe.", "Keep reset and write/programming operations disabled during identification.", "Stop if target voltage or wiring is ambiguous."], ProbeRisk.ReadOnly);

    private static ProbePlan BuildSerialPlan() => new(ProbeTransport.Serial,
        [new("GND", "GND", "Common reference"), new("RX", "Target TX", "Receive target output"), new("TX", "Target RX", "Transmit probe output")],
        ["Start with receive-only observation where possible.", "Detect common UART framing from observed transitions.", "Test conservative baud candidates only after the user confirms TX is safe.", "Classify readable banners or responses without assuming the device identity."],
        ["Never connect a probe TX to an unknown voltage domain without verification.", "Do not assume three-point UART is three-point logic voltage."], ProbeRisk.ReadOnly);

    private static ProbePlan BuildPicoPlan() => new(ProbeTransport.PicoBridge,
        [new("GND", "GND", "Common reference"), new("Target Vref", "ADC-capable GPIO", "Voltage measurement only"), new("Candidate signal", "GPIO", "User-selected probe point")],
        ["Use the RP2040 as a measurement/bridge device.", "Measure target voltage before driving any GPIO.", "Begin with high-impedance observation.", "Classify digital activity and, where appropriate, estimate serial timing.", "Only enable active drive after an explicit user-selected protocol and voltage level are known."],
        ["Never drive an unknown target pin during automatic discovery.", "Keep GPIOs high impedance until the target electrical level is established.", "Add level shifting when target voltage is outside the RP2040 GPIO range."], ProbeRisk.ReadOnly);
}
