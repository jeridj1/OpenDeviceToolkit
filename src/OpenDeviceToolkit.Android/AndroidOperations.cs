using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Android;

public sealed class AndroidOperations
{
    private readonly AdbManager _adb;

    public AndroidOperations(AdbManager adb) => _adb = adb;

    public static IReadOnlyList<DeviceOperation> Catalog { get; } =
    [
        new("android.shell.read", "ADB Shell", "Run an explicitly supplied ADB shell command.", OperationRisk.Reversible),
        new("android.reboot", "Reboot device", "Restart the Android device through ADB.", OperationRisk.Reversible, true),
        new("android.reboot.bootloader", "Reboot to bootloader", "Restart the device into its bootloader/fastboot mode.", OperationRisk.Reversible, true),
        new("android.reboot.recovery", "Reboot to recovery", "Restart the device into recovery mode.", OperationRisk.Reversible, true),
        new("android.enable.adb_root", "Enable ADB root", "Request an ADB root daemon where supported.", OperationRisk.Destructive, true)
    ];

    public async Task<CommandResult> ShellAsync(OperationContext context, string command, CancellationToken cancellationToken = default)
    {
        var operation = Catalog.Single(x => x.Id == "android.shell.read");
        DeviceOperationGuard.Validate(operation, context);
        return await _adb.RunShellAsync(context.DeviceSerial, command, cancellationToken);
    }

    public Task<CommandResult> RebootAsync(OperationContext context, CancellationToken cancellationToken = default)
        => RunAdbOperationAsync(context, "android.reboot", "reboot", cancellationToken);

    public Task<CommandResult> RebootBootloaderAsync(OperationContext context, CancellationToken cancellationToken = default)
        => RunAdbOperationAsync(context, "android.reboot.bootloader", "reboot bootloader", cancellationToken);

    public Task<CommandResult> RebootRecoveryAsync(OperationContext context, CancellationToken cancellationToken = default)
        => RunAdbOperationAsync(context, "android.reboot.recovery", "reboot recovery", cancellationToken);

    public Task<CommandResult> EnableAdbRootAsync(OperationContext context, CancellationToken cancellationToken = default)
        => RunAdbOperationAsync(context, "android.enable.adb_root", "root", cancellationToken);

    private async Task<CommandResult> RunAdbOperationAsync(OperationContext context, string operationId, string command, CancellationToken cancellationToken)
    {
        var operation = Catalog.Single(x => x.Id == operationId);
        DeviceOperationGuard.Validate(operation, context);
        return await _adb.RunAsync(context.DeviceSerial, command, cancellationToken);
    }
}
