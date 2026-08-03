using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Android;

public sealed record AndroidPackageResult(bool Success, string Message, string Output = "");

/// <summary>
/// Controlled package-management operations through the standard Android package manager.
/// No root escalation or security-bypass behavior is attempted here.
/// </summary>
public sealed class AndroidPackageService
{
    private readonly CommandRunner _runner;
    private readonly string _adbPath;

    public AndroidPackageService(CommandRunner runner, string adbPath = "adb")
    {
        _runner = runner;
        _adbPath = string.IsNullOrWhiteSpace(adbPath) ? "adb" : adbPath;
    }

    public async Task<AndroidPackageResult> ListPackagesAsync(AndroidDevice device, bool thirdPartyOnly = false, CancellationToken cancellationToken = default)
    {
        if (device.State != DeviceConnectionState.Connected)
            return new(false, "The device is not connected through ADB.");
        var args = $"-s {Quote(device.Serial)} shell pm list packages" + (thirdPartyOnly ? " -3" : "");
        var result = await _runner.RunAsync(_adbPath, args, timeout: TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);
        return new(result.Success, result.Success ? "Package enumeration completed." : result.StandardError.Trim(), result.StandardOutput);
    }

    public async Task<AndroidPackageResult> InstallApkAsync(AndroidDevice device, string apkPath, bool explicitConfirmation, CancellationToken cancellationToken = default)
    {
        if (device.State != DeviceConnectionState.Connected)
            return new(false, "The device is not connected through ADB.");
        if (!explicitConfirmation)
            return new(false, "Explicit confirmation is required before installing an APK.");
        var source = Path.GetFullPath(apkPath);
        if (!File.Exists(source))
            return new(false, $"APK does not exist: {source}");
        if (!source.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            return new(false, "The selected file does not have an APK extension.");
        var result = await _runner.RunAsync(_adbPath, $"-s {Quote(device.Serial)} install {Quote(source)}", timeout: TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);
        return new(result.Success, result.Success ? "APK installation completed." : result.StandardError.Trim(), result.StandardOutput);
    }

    private static string Quote(string value) => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}
