using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Android;

public sealed class AdbManager
{
    private readonly CommandRunner _runner;
    private readonly string _adbPath;

    public AdbManager(CommandRunner runner, string? adbPath = null)
    {
        _runner = runner;
        _adbPath = string.IsNullOrWhiteSpace(adbPath) ? "adb" : adbPath;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _runner.RunAsync(_adbPath, "version", timeout: TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
            return result.Success;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<(string Serial, DeviceConnectionState State)>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _runner.RunAsync(_adbPath, "devices", timeout: TimeSpan.FromSeconds(10), cancellationToken: cancellationToken);
        if (!result.Success)
            return Array.Empty<(string, DeviceConnectionState)>();

        var devices = new List<(string, DeviceConnectionState)>();
        foreach (var rawLine in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var state = parts[1].Trim().ToLowerInvariant() switch
            {
                "device" => DeviceConnectionState.Connected,
                "unauthorized" => DeviceConnectionState.Unauthorized,
                "offline" => DeviceConnectionState.Offline,
                _ => DeviceConnectionState.Unknown
            };
            devices.Add((parts[0].Trim(), state));
        }

        return devices;
    }

    public async Task<AndroidDevice?> InspectAsync(string serial, CancellationToken cancellationToken = default)
    {
        var result = await _runner.RunAsync(_adbPath, $"-s {Quote(serial)} shell getprop", timeout: TimeSpan.FromSeconds(15), cancellationToken: cancellationToken);
        if (!result.Success)
            return null;

        var properties = ParseProperties(result.StandardOutput);
        return new AndroidDevice(serial, DeviceConnectionState.Connected, properties);
    }

    public async Task<CommandResult> RunShellAsync(string serial, string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("A shell command is required.", nameof(command));

        return await _runner.RunAsync(_adbPath, $"-s {Quote(serial)} shell {command}", timeout: TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);
    }

    public static IReadOnlyDictionary<string, string> ParseProperties(string text)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('['))
                continue;

            var separator = trimmed.IndexOf("]: [", StringComparison.Ordinal);
            if (separator < 1 || !trimmed.EndsWith(']'))
                continue;

            var key = trimmed[1..separator];
            var valueStart = separator + 4;
            var value = trimmed[valueStart..^1];
            properties[key] = value;
        }
        return properties;
    }

    private static string Quote(string value) => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}
