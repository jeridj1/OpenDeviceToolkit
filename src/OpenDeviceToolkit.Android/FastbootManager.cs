using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Android;

public enum FastbootConnectionState
{
    Fastboot,
    Unknown
}

/// <summary>
/// Drives the <c>fastboot</c> bootloader tool, analogous to <see cref="AdbManager"/>
/// for ADB. Reuses <see cref="CommandRunner"/> for process execution and keeps
/// parsing logic in pure static helpers so it can be unit-tested without a device
/// or the fastboot binary present.
/// </summary>
public sealed class FastbootManager
{
    private readonly CommandRunner _runner;
    private readonly string _fastbootPath;

    public FastbootManager(CommandRunner runner, string? fastbootPath = null)
    {
        _runner = runner;
        _fastbootPath = string.IsNullOrWhiteSpace(fastbootPath) ? "fastboot" : fastbootPath;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _runner.RunAsync(_fastbootPath, "version", timeout: TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
            return result.Success;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<(string Serial, FastbootConnectionState State)>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _runner.RunAsync(_fastbootPath, "devices", timeout: TimeSpan.FromSeconds(10), cancellationToken: cancellationToken);
        if (!result.Success)
            return Array.Empty<(string, FastbootConnectionState)>();

        return ParseDevices(result.StandardOutput);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetVarAsync(string serial, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial))
            throw new ArgumentException("A device serial is required.", nameof(serial));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A variable name is required.", nameof(name));

        var result = await _runner.RunAsync(_fastbootPath, "-s " + serial + " getvar " + name, timeout: TimeSpan.FromSeconds(10), cancellationToken: cancellationToken);
        return ParseGetVar(result.StandardOutput);
    }

    public async Task<CommandResult> RunAsync(string serial, string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial))
            throw new ArgumentException("A device serial is required.", nameof(serial));
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("A fastboot command is required.", nameof(command));

        return await _runner.RunAsync(_fastbootPath, "-s " + serial + " " + command, timeout: TimeSpan.FromSeconds(60), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Parses <c>fastboot devices</c> output. Each non-header line is the serial
    /// and its state (typically <c>fastboot</c>), separated by whitespace.
    /// </summary>
    public static IReadOnlyList<(string Serial, FastbootConnectionState State)> ParseDevices(string text)
    {
        var devices = new List<(string, FastbootConnectionState)>();
        foreach (var rawLine in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var state = parts[1].Trim().ToLowerInvariant() switch
            {
                "fastboot" => FastbootConnectionState.Fastboot,
                _ => FastbootConnectionState.Unknown
            };
            devices.Add((parts[0].Trim(), state));
        }
        return devices;
    }

    /// <summary>
    /// Parses <c>fastboot getvar</c> output. Each variable line is
    /// <c>name: value</c>; informational lines (Finished/FAILED) are skipped.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ParseGetVar(string text)
    {
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;
            if (line.StartsWith("Finished", StringComparison.OrdinalIgnoreCase))
                continue;
            if (line.StartsWith("FAILED", StringComparison.OrdinalIgnoreCase))
                continue;

            var sep = line.IndexOf(':');
            if (sep <= 0)
                continue;

            var key = line[..sep].Trim();
            var value = line[(sep + 1)..].Trim();
            if (key.Length > 0)
                vars[key] = value;
        }
        return vars;
    }
}
