namespace OpenDeviceToolkit.Android;

public enum AndroidOperationKind
{
    ReadOnlyShell,
    RebootSystem,
    RebootBootloader,
    RebootRecovery
}

public sealed record AndroidOperationResult(AndroidOperationKind Kind, bool Executed, string Message);

/// <summary>
/// Central gate for Android operations. Read-only inspection is separate from
/// state-changing operations so the UI can require explicit confirmation later.
/// All confirmation/readiness decisions funnel through <see cref="OperationGuard"/>.
/// </summary>
public sealed class AndroidOperationService
{
    private readonly AdbManager _adb;

    public AndroidOperationService(AdbManager adb) => _adb = adb;

    /// <summary>
    /// Builds a <see cref="PlannedOperation"/> describing the requested operation so the
    /// centralized <see cref="OperationGuard"/> (rather than ad-hoc inline checks) decides
    /// whether it may proceed. Read-only shell queries are classified as
    /// <see cref="OperationRisk.ReadOnly"/>; reboots are <see cref="OperationRisk.StateChange"/>.
    /// </summary>
    public static PlannedOperation PlanFor(AndroidDevice device, AndroidOperationKind kind)
    {
        var ready = device.State == DeviceConnectionState.Connected;
        var risk = kind == AndroidOperationKind.ReadOnlyShell ? OperationRisk.ReadOnly : OperationRisk.StateChange;
        return new PlannedOperation(
            kind.ToString(),
            risk,
            ready,
            new[] { "ADB device state must be Connected" },
            ready ? "ADB is online." : "The device is not currently connected through ADB.");
    }

    public async Task<AndroidOperationResult> RebootAsync(AndroidDevice device, AndroidOperationKind kind, bool explicitConfirmation, CancellationToken cancellationToken = default)
    {
        if (kind == AndroidOperationKind.ReadOnlyShell)
            return new(kind, false, "Use the read-only inspection service for shell queries.");

        try
        {
            OperationGuard.Require(PlanFor(device, kind), device.Serial, explicitConfirmation);
        }
        catch (OperationBlockedException ex)
        {
            return new(kind, false, ex.Message);
        }

        var command = kind switch
        {
            AndroidOperationKind.RebootSystem => "reboot",
            AndroidOperationKind.RebootBootloader => "reboot bootloader",
            AndroidOperationKind.RebootRecovery => "reboot recovery",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        var result = await _adb.RunShellAsync(device.Serial, command, cancellationToken);
        return new(kind, result.Success, result.Success
            ? $"ADB accepted '{command}'. The device may disconnect while rebooting."
            : $"ADB rejected '{command}': {result.StandardError.Trim()}");
    }
}
