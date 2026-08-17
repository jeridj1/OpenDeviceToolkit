using System.Text.Json;

namespace OpenDeviceToolkit.Android;

public sealed record AndroidSnapshot(string CreatedUtc, string Serial, string Manufacturer, string Model, string AndroidVersion, string SecurityPatch, string Platform, string BootState, bool? FlashLocked, string Slot, IReadOnlyDictionary<string, string> Properties, IReadOnlyList<AndroidDiagnosticResult> Diagnostics);

/// <summary>Captures device state so later scans can be compared without modifying the phone.</summary>
public sealed class AndroidSnapshotService
{
    public AndroidSnapshot Capture(AndroidDevice device, IReadOnlyList<AndroidDiagnosticResult> diagnostics) => new(DateTimeOffset.UtcNow.ToString("O"), device.Serial, device.Manufacturer, device.Model, device.AndroidVersion, device.SecurityPatch, device.Platform, device.BootState, ParseFlashLocked(device.FlashLocked), device.Slot, new Dictionary<string, string>(device.Properties), diagnostics.ToArray());

    public string Save(AndroidSnapshot snapshot, string directory)
    {
        Directory.CreateDirectory(directory);
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var safeSerial = string.Concat(snapshot.Serial.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        var path = Path.Combine(directory, $"android-snapshot-{safeSerial}-{stamp}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private static bool? ParseFlashLocked(string value) => value.Trim().ToLowerInvariant() switch
    {
        "1" or "true" or "yes" => true,
        "0" or "false" or "no" => false,
        _ => null
    };

    public static IReadOnlyList<string> Compare(AndroidSnapshot before, AndroidSnapshot after)
    {
        var changes = new List<string>();
        if (before.AndroidVersion != after.AndroidVersion) changes.Add($"Android version: '{before.AndroidVersion}' -> '{after.AndroidVersion}'");
        if (before.SecurityPatch != after.SecurityPatch) changes.Add($"Security patch: '{before.SecurityPatch}' -> '{after.SecurityPatch}'");
        if (before.BootState != after.BootState) changes.Add($"Verified boot: '{before.BootState}' -> '{after.BootState}'");
        if (before.FlashLocked != after.FlashLocked) changes.Add($"Flash lock: '{before.FlashLocked}' -> '{after.FlashLocked}'");
        if (before.Slot != after.Slot) changes.Add($"Boot slot: '{before.Slot}' -> '{after.Slot}'");
        var keys = before.Properties.Keys.Union(after.Properties.Keys).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
        foreach (var key in keys)
        {
            before.Properties.TryGetValue(key, out var oldValue);
            after.Properties.TryGetValue(key, out var newValue);
            if (oldValue != newValue) changes.Add($"Property {key}: '{oldValue}' -> '{newValue}'");
        }
        return changes;
    }
}
