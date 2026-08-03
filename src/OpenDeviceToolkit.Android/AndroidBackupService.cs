using System.Security.Cryptography;
using System.Text;

namespace OpenDeviceToolkit.Android;

public sealed record AndroidBackupResult(bool Success, string Message, string? BackupPath = null, string? Sha256 = null);

/// <summary>
/// Creates evidence-preserving ADB pulls with SHA-256 verification.
/// The service does not unlock, root, or modify the device.
/// </summary>
public sealed class AndroidBackupService
{
    private readonly AndroidFileService _files;

    public AndroidBackupService(AndroidFileService files) => _files = files;

    public async Task<AndroidBackupResult> BackupFileAsync(AndroidDevice device, string remotePath, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (device.State != DeviceConnectionState.Connected)
            return new(false, "The device is not connected through ADB.");
        if (string.IsNullOrWhiteSpace(remotePath))
            return new(false, "A remote path is required.");

        Directory.CreateDirectory(destinationDirectory);
        var safeName = Path.GetFileName(remotePath.Replace('/', Path.DirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "adb-backup.bin";
        var target = Path.Combine(Path.GetFullPath(destinationDirectory), safeName);
        var pull = await _files.PullAsync(device, remotePath, target, cancellationToken);
        if (!pull.Success || string.IsNullOrWhiteSpace(pull.LocalPath) || !File.Exists(pull.LocalPath))
            return new(false, pull.Message, pull.LocalPath);

        await using var stream = File.OpenRead(pull.LocalPath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        var sha = Convert.ToHexString(hash).ToLowerInvariant();
        var manifest = Path.Combine(Path.GetDirectoryName(pull.LocalPath)!, Path.GetFileName(pull.LocalPath) + ".sha256");
        await File.WriteAllTextAsync(manifest, $"{sha}  {Path.GetFileName(pull.LocalPath)}{Environment.NewLine}", Encoding.UTF8, cancellationToken);
        return new(true, $"Backup completed and SHA-256 recorded at '{manifest}'.", pull.LocalPath, sha);
    }
}
