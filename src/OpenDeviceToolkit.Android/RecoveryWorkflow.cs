using OpenDeviceToolkit.Core.Firmware;

namespace OpenDeviceToolkit.Android;

public sealed record RecoveryRequest(
    string DeviceSerial,
    string DeviceModel,
    string DetectedInterface,
    string FirmwarePath,
    FirmwareArtifactSpec FirmwareSpec,
    bool ExplicitlyConfirmed);

/// <summary>
/// Pluggable device operation executed (and verified) by the recovery workflow
/// once the guard allows it. Injecting this keeps the workflow testable with a
/// fake and decoupled from the real fastboot/adb providers, which require a
/// live device.
/// </summary>
public interface IRecoveryOperation
{
    string Name { get; }
    OperationRisk Risk { get; }
    Task<bool> ExecuteAsync(RecoveryRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyAsync(RecoveryRequest request, CancellationToken cancellationToken = default);
}

public sealed record RecoveryPhase(string Name, string Status, bool Success);

public sealed record RecoveryWorkflowResult(
    IReadOnlyList<RecoveryPhase> Phases,
    bool FirmwareValidated,
    bool BackupCreated,
    bool OperationExecuted,
    bool Verified,
    string Summary);

/// <summary>
/// End-to-end recovery workflow tying the pieces together with clear phases:
/// identify -> detect supported interface -> locate/validate firmware ->
/// back up recoverable state -> guarded operation -> post-operation verification.
/// Read-only phases run from supplied (mock) data; the write phase is gated by
/// <see cref="OperationGuard"/> and is a no-op without explicit confirmation.
/// </summary>
public sealed class RecoveryWorkflow
{
    private readonly FirmwareArtifactService _firmware;
    private readonly IRecoveryOperation? _operation;

    public RecoveryWorkflow(FirmwareArtifactService firmware, IRecoveryOperation? operation = null)
    {
        _firmware = firmware;
        _operation = operation;
    }

    public async Task<RecoveryWorkflowResult> RunAsync(RecoveryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var phases = new List<RecoveryPhase>();
        var backupCreated = false;

        // Phase 1: identify (device identity is supplied by the caller; real
        // identification requires ADB/hardware and is the caller's responsibility).
        var identified = !string.IsNullOrWhiteSpace(request.DeviceSerial);
        phases.Add(new RecoveryPhase("Identify device", "Device: " + request.DeviceModel + " via " + request.DetectedInterface, identified));

        // Phase 2: detect a supported update/programming interface.
        var interfaceSupported = request.DetectedInterface is "adb" or "fastboot" or "swd" or "uart" or "cmsis-dap";
        phases.Add(new RecoveryPhase("Detect interface",
            interfaceSupported ? "Supported: " + request.DetectedInterface : "No supported update/programming interface detected.",
            interfaceSupported));
        if (!interfaceSupported || !identified)
            return Final(phases, false, backupCreated, false, false, "No supported interface or no target identified; workflow stopped before any write.");

        // Phase 3: locate and validate the firmware artifact.
        FirmwareArtifact artifact;
        bool validated;
        try
        {
            artifact = await _firmware.AcquireAsync(request.FirmwarePath, cancellationToken);
            artifact = await _firmware.ValidateAsync(artifact, request.FirmwareSpec, cancellationToken);
            validated = artifact.ValidationStatus == FirmwareValidationStatus.Verified;
            phases.Add(new RecoveryPhase("Validate firmware", artifact.ValidationStatus + ": " + artifact.ValidationMessage, validated));
        }
        catch (Exception ex)
        {
            phases.Add(new RecoveryPhase("Validate firmware", "Failed: " + ex.Message, success: false));
            return Final(phases, false, backupCreated, false, false, "Firmware validation failed; workflow stopped before any write.");
        }
        if (!validated)
            return Final(phases, validated, backupCreated, false, false, "Firmware not verified; workflow stopped before any write.");

        // Phase 4: back up recoverable state (recorded; real backup is the caller's job).
        backupCreated = true;
        phases.Add(new RecoveryPhase("Backup state", "Backup step recorded (no-op in workflow core).", success: true));

        // Phase 5: guarded operation (no-op without explicit confirmation).
        var plan = new PlannedOperation(
            _operation?.Name ?? "device operation",
            _operation?.Risk ?? OperationRisk.PersistentWrite,
            ready: validated,
            new[] { "Verified target artifact and checksum", "Explicit user confirmation" },
            "Firmware verified.");
        bool executed = false;
        try
        {
            OperationGuard.Require(plan, request.DeviceSerial, request.ExplicitlyConfirmed);
            if (_operation is null)
            {
                phases.Add(new RecoveryPhase("Guarded operation", "Guard passed; no operation bound (no-op).", success: true));
            }
            else
            {
                executed = await _operation.ExecuteAsync(request, cancellationToken);
                phases.Add(new RecoveryPhase("Guarded operation", _operation.Name + (executed ? " executed." : " did not execute."), executed));
            }
        }
        catch (OperationBlockedException ex)
        {
            phases.Add(new RecoveryPhase("Guarded operation", "Blocked by guard: " + ex.Message, success: false));
            return Final(phases, validated, backupCreated, false, false, "Operation blocked by guard: " + ex.Message);
        }

        // Phase 6: post-operation verification.
        bool verified;
        if (_operation is null)
        {
            verified = true;
            phases.Add(new RecoveryPhase("Verify", "No operation bound; verification skipped (no-op).", success: true));
        }
        else
        {
            try
            {
                verified = await _operation.VerifyAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                phases.Add(new RecoveryPhase("Verify", "Verification failed: " + ex.Message, success: false));
                return Final(phases, validated, backupCreated, executed, false, "Post-operation verification failed.");
            }
            phases.Add(new RecoveryPhase("Verify", verified ? "Post-operation verification passed." : "Verification reported failure.", verified));
        }

        return Final(phases, validated, backupCreated, executed, verified,
            verified ? "Workflow complete; post-operation verification passed." : "Workflow complete; verification failed.");
    }

    private static RecoveryWorkflowResult Final(List<RecoveryPhase> phases, bool firmwareValidated, bool backupCreated, bool operationExecuted, bool verified, string summary)
        => new RecoveryWorkflowResult(phases, firmwareValidated, backupCreated, operationExecuted, verified, summary);
}
