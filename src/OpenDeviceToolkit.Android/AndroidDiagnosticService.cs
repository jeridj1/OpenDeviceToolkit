namespace OpenDeviceToolkit.Android;

public sealed record AndroidDiagnosticResult(string Name, bool Success, string Output, TimeSpan Duration);

public sealed class AndroidDiagnosticService
{
    private readonly AdbManager _adb;

    public AndroidDiagnosticService(AdbManager adb) => _adb = adb;

    public async Task<IReadOnlyList<AndroidDiagnosticResult>> RunReadOnlyAsync(
        string serial,
        CancellationToken cancellationToken = default)
    {
        var commands = new[]
        {
            ("Boot mode", "getprop ro.bootmode"),
            ("Bootloader", "getprop ro.bootloader"),
            ("Boot reason", "getprop ro.boot.bootreason"),
            ("Boot slot", "getprop ro.boot.slot_suffix"),
            ("Verified boot", "getprop ro.boot.verifiedbootstate"),
            ("Flash lock state", "getprop ro.boot.flash.locked"),
            ("Build fingerprint", "getprop ro.build.fingerprint"),
            ("USB configuration", "getprop sys.usb.config"),
            ("USB state", "getprop sys.usb.state"),
            ("Named partitions", "ls -la /dev/block/by-name"),
            ("Kernel partition table", "cat /proc/partitions"),
            ("Data filesystem", "df -h /data")
        };

        var results = new List<AndroidDiagnosticResult>();
        foreach (var (name, command) in commands)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                var result = await _adb.RunShellAsync(serial, command, cancellationToken);
                var output = string.IsNullOrWhiteSpace(result.StandardOutput)
                    ? result.StandardError.Trim()
                    : result.StandardOutput.Trim();
                results.Add(new AndroidDiagnosticResult(name, result.Success, output, result.Duration));
            }
            catch (Exception ex)
            {
                var duration = System.Diagnostics.Stopwatch.GetElapsedTime(started);
                results.Add(new AndroidDiagnosticResult(name, false, ex.Message, duration));
            }
        }

        return results;
    }
}
