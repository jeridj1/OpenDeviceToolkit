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
/// </summary>
public sealed class AndroidOperationService
{
    private readonly AdbManager _adb;

    public AndroidOperationService(AdbManager adb) => _adb = adb;

    public async Task<AndroidOperationResult> RebootAsync(AndroidDevice device, AndroidOperationKind kind, bool explicitConfirmation, CancellationToken cancellationToken = default)
    {
        if (kind == AndroidOperationKind.ReadOnlyShell)
            return new(kind, false, "Use the read-only inspection service for shell queries.");
        if (device.State != DeviceConnectionState.Connected)
            return new(kind, false, "The device is not currently connected through ADB.");
        if (!explicitConfirmation)
            return new(kind, false, "Explicit confirmation is required before a reboot operation.");

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
