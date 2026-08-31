using OpenDeviceToolkit.Hardware.Rp2040;

namespace OpenDeviceToolkit.Hardware;

public sealed record ProbeWorkflowObservation(string Name, string Value, bool Success);
public sealed record ProbeWorkflowCapability(string Name, string Evidence, bool Available);

public sealed record ProbeWorkflowResult(
    string TargetDescription,
    IReadOnlyList<string> ConnectionGuidance,
    bool Connected,
    IReadOnlyList<ProbeWorkflowObservation> Observations,
    IReadOnlyList<ProbeWorkflowCapability> Capabilities,
    IReadOnlyList<string> Notes);

/// <summary>
/// Guided RP2040 probe workflow. It (1) tells the user what to connect, (2) tests the
/// connection, (3) captures read-only observations (voltage, logic activity), and
/// (4) infers which operations are possible from the evidence — never assuming a
/// capability that the observations do not support. Built over the existing
/// <see cref="ProbePlanner"/> and <see cref="IRp2040Controller"/> stack.
/// </summary>
public sealed class ProbeWorkflow
{
    private readonly IRp2040Controller _controller;
    private readonly ProbePlanner _planner = new();

    public ProbeWorkflow(IRp2040Controller controller) => _controller = controller;

    /// <summary>
    /// Infers capabilities purely from observed voltage and any known chip pinout.
    /// Kept static so it can be unit-tested without a controller or hardware.
    /// </summary>
    public static IReadOnlyList<ProbeWorkflowCapability> InferCapabilities(double? voltage, ChipPinout? pinout)
    {
        var caps = new List<ProbeWorkflowCapability>();

        var voltageKnown = voltage is > 0 and <= 5.5;
        caps.Add(new ProbeWorkflowCapability(
            "Read-only GPIO/logic observation",
            voltageKnown ? "Target voltage within measurable range." : "Voltage unknown.",
            voltageKnown));

        var inRp2040Domain = voltage is >= 1.8 and <= 3.6;
        caps.Add(new ProbeWorkflowCapability(
            "Direct GPIO drive (no level shifting)",
            inRp2040Domain ? "Target voltage in RP2040 1.8-3.6V domain." : "Voltage outside the RP2040 GPIO domain; level shifting required.",
            inRp2040Domain));

        if (pinout is not null)
        {
            foreach (var iface in pinout.ProgrammingInterfaces)
                caps.Add(new ProbeWorkflowCapability(
                    iface.Type + " (known pinout)",
                    "Pinout for " + pinout.Name + " is in the database.",
                    Available: true));
        }
        else
        {
            caps.Add(new ProbeWorkflowCapability(
                "Known-chip programming interface",
                "No matching pinout in the database.",
                Available: false));
        }

        return caps;
    }

    public async Task<ProbeWorkflowResult> RunAsync(ProbeTarget target, CancellationToken cancellationToken = default)
    {
        // Phase 1: what to connect (reuse the existing planner).
        var plan = _planner.Build(target);
        var guidance = new List<string>();
        guidance.AddRange(plan.Steps);
        guidance.Add("Connections:");
        foreach (var c in plan.Connections)
            guidance.Add("  " + c.Signal + " -> " + c.Pin + " (" + c.Purpose + ")");
        guidance.AddRange(plan.SafetyNotes.Select(n => "Safety: " + n));

        // Phase 2: test the connection.
        var connected = false;
        try
        {
            connected = await _controller.ConnectAsync(cancellationToken);
        }
        catch
        {
            connected = _controller.IsConnected;
        }

        // Phase 3: capture read-only observations.
        var observations = new List<ProbeWorkflowObservation>();
        double? voltage = null;
        if (connected)
        {
            try
            {
                voltage = await _controller.MeasureVoltageAsync(26, cancellationToken);
            }
            catch
            {
                voltage = null;
            }
            observations.Add(new ProbeWorkflowObservation("Target voltage (GPIO26)", voltage is > 0 ? voltage.Value.ToString("0.0") + "V" : "unknown", voltage is > 0));
            observations.Add(new ProbeWorkflowObservation("Controller mode", _controller.CurrentMode.ToString(), Success: true));
            observations.Add(new ProbeWorkflowObservation("Available modes", string.Join(", ", _controller.AvailableModes), Success: true));

            try
            {
                var capture = await _controller.CaptureLogicAsync(new[] { 0, 1 }, TimeSpan.FromMilliseconds(10), 1000, cancellationToken);
                observations.Add(new ProbeWorkflowObservation("Logic activity (GP0/GP1)", capture.Samples.Count + " samples", capture.Samples.Count > 0));
            }
            catch (Exception ex)
            {
                observations.Add(new ProbeWorkflowObservation("Logic activity (GP0/GP1)", "capture failed: " + ex.Message, Success: false));
            }
        }

        // Phase 4: infer possible operations from the evidence.
        var pinout = PinoutDatabase.GetPinout(target.Description);
        var capabilities = InferCapabilities(voltage, pinout);

        var notes = new List<string>();
        if (!connected)
            notes.Add("Could not establish a connection; observations and capabilities are limited.");
        if (voltage is null or 0)
            notes.Add("Target voltage unknown; keep all target-facing GPIO high-impedance until measured.");
        if (voltage is > 0 and (< 1.8 or > 3.6))
            notes.Add("Target voltage outside the RP2040 GPIO domain; use level shifting before any drive.");

        return new ProbeWorkflowResult(target.Description, guidance, connected, observations, capabilities, notes);
    }
}
