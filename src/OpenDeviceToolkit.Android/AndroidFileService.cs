using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Android;

public sealed record AndroidFileOperationResult(bool Success, string Message, string? LocalPath = null);

/// <summary>
/// Explicit ADB file transfer operations. Pull is non-destructive; push requires
/// caller confirmation and never attempts to elevate privileges on the device.
/// </summary>
public sealed class AndroidFileService
{
    private readonly CommandRunner _runner;
    private readonly string _adbPath;

    public AndroidFileService(CommandRunner runner, string adbPath = "adb")
    {
        _runner = runner;
        _adbPath = string.IsNullOrWhiteSpace(adbPath) ? "adb" : adbPath;
    }

    public async Task<AndroidFileOperationResult> PullAsync(AndroidDevice device, string remotePath, string localPath, CancellationToken cancellationToken = default)
    {
        if (!IsConnected(device) || string.IsNullOrWhiteSpace(remotePath) || string.IsNullOrWhiteSpace(localPath))
            return new(false, "A connected device, remote path, and local path are required.");

        var destination = Path.GetFullPath(localPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var result = await _runner.RunAsync(_adbPath, $"-s {Quote(device.Serial)} pull {Quote(remotePath)} {Quote(destination)}", timeout: TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);
        return new(result.Success, result.Success ? $"Pulled '{remotePath}' to '{destination}'." : result.StandardError.Trim(), destination);
    }

    public async Task<AndroidFileOperationResult> PushAsync(AndroidDevice device, string localPath, string remotePath, bool explicitConfirmation, CancellationToken cancellationToken = default)
    {
        if (!IsConnected(device) || string.IsNullOrWhiteSpace(localPath) || string.IsNullOrWhiteSpace(remotePath))
            return new(false, "A connected device, local path, and remote path are required.");
        if (!explicitConfirmation)
            return new(false, "Explicit confirmation is required before writing a file to the device.");

        var source = Path.GetFullPath(localPath);
        if (!File.Exists(source))
            return new(false, $"Local file does not exist: {source}");

        var result = await _runner.RunAsync(_adbPath, $"-s {Quote(device.Serial)} push {Quote(source)} {Quote(remotePath)}", timeout: TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);
        return new(result.Success, result.Success ? $"Pushed '{source}' to '{remotePath}'." : result.StandardError.Trim());
    }

    private static bool IsConnected(AndroidDevice device) => device.State == DeviceConnectionState.Connected;
    private static string Quote(string value) => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}
