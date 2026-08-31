namespace OpenDeviceToolkit.Android;

public sealed record FastbootOperationResult(string Name, bool Executed, string Message);

/// <summary>
/// Guarded fastboot operations. Every state-changing operation funnels through
/// <see cref="OperationGuard"/>: read-only queries (getvar, oem device-info) only
/// require an identified target; reboots are <see cref="OperationRisk.StateChange"/>
/// and require confirmation; <c>flash</c>/<c>erase</c> are
/// <see cref="OperationRisk.PersistentWrite"/> and require a verified artifact
/// plus explicit confirmation.
/// </summary>
public sealed class FastbootOperationService
{
    private readonly FastbootManager _fastboot;

    public FastbootOperationService(FastbootManager fastboot) => _fastboot = fastboot;

    /// <summary>
    /// Builds a <see cref="PlannedOperation"/> for a fastboot action so the
    /// centralized <see cref="OperationGuard"/> decides whether it may proceed.
    /// </summary>
    public static PlannedOperation PlanFor(string name, OperationRisk risk, bool ready, string reason)
    {
        var preconditions = risk == OperationRisk.ReadOnly
            ? new[] { "Device must be in fastboot mode" }
            : new[] { "Device must be in fastboot mode", "Verified target artifact and checksum", "Explicit user confirmation" };
        return new PlannedOperation(name, risk, ready, preconditions, reason);
    }

    public async Task<FastbootOperationResult> GetVarAsync(string serial, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            OperationGuard.Require(PlanFor("getvar " + name, OperationRisk.ReadOnly, TargetReady(serial), "A target device is required."), serial, explicitlyConfirmed: false);
        }
        catch (OperationBlockedException ex)
        {
            return new("getvar " + name, false, ex.Message);
        }

        var vars = await _fastboot.GetVarAsync(serial, name, cancellationToken);
        var value = vars.TryGetValue(name, out var v) ? v : "(not set)";
        return new("getvar " + name, true, name + ": " + v is null ? "(not set)" : value);
    }

    public async Task<FastbootOperationResult> OemDeviceInfoAsync(string serial, CancellationToken cancellationToken = default)
    {
        try
        {
            OperationGuard.Require(PlanFor("oem device-info", OperationRisk.ReadOnly, TargetReady(serial), "A target device is required."), serial, explicitlyConfirmed: false);
        }
        catch (OperationBlockedException ex)
        {
            return new("oem device-info", false, ex.Message);
        }

        var result = await _fastboot.RunAsync(serial, "oem device-info", cancellationToken);
        return new("oem device-info", result.Success, result.Success ? result.StandardOutput.Trim() : result.StandardError.Trim());
    }

    public async Task<FastbootOperationResult> RebootAsync(string serial, bool explicitlyConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            OperationGuard.Require(PlanFor("reboot", OperationRisk.StateChange, TargetReady(serial), "A target device is required."), serial, explicitlyConfirmed);
        }
        catch (OperationBlockedException ex)
        {
            return new("reboot", false, ex.Message);
        }

        var result = await _fastboot.RunAsync(serial, "reboot", cancellationToken);
        return new("reboot", result.Success, result.Success ? "Fastboot accepted reboot." : result.StandardError.Trim());
    }

    public async Task<FastbootOperationResult> FlashAsync(string serial, string partition, string artifactPath, bool artifactVerified, bool explicitlyConfirmed, CancellationToken cancellationToken = default)
    {
        var ready = TargetReady(serial) && artifactVerified;
        try
        {
            OperationGuard.Require(PlanFor("flash " + partition, OperationRisk.PersistentWrite, ready, artifactVerified ? "Artifact verified." : "Artifact has not been verified."), serial, explicitlyConfirmed);
        }
        catch (OperationBlockedException ex)
        {
            return new("flash " + partition, false, ex.Message);
        }

        var result = await _fastboot.RunAsync(serial, "flash " + partition + " " + '"' + artifactPath + '"', cancellationToken);
        return new("flash " + partition, result.Success, result.Success ? "Flashed " + partition + "." : result.StandardError.Trim());
    }

    public async Task<FastbootOperationResult> EraseAsync(string serial, string partition, bool explicitlyConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            OperationGuard.Require(PlanFor("erase " + partition, OperationRisk.PersistentWrite, TargetReady(serial), "A target device is required."), serial, explicitlyConfirmed);
        }
        catch (OperationBlockedException ex)
        {
            return new("erase " + partition, false, ex.Message);
        }

        var result = await _fastboot.RunAsync(serial, "erase " + partition, cancellationToken);
        return new("erase " + partition, result.Success, result.Success ? "Erased " + partition + "." : result.StandardError.Trim());
    }

    private static bool TargetReady(string serial) => !string.IsNullOrWhiteSpace(serial);
}
