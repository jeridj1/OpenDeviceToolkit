namespace OpenDeviceToolkit.Android;

public enum CapabilityStatus
{
    Available,
    Unavailable,
    Unknown,
    RequiresPrivilege
}

public sealed record AndroidCapability(string Name, CapabilityStatus Status, string Evidence, string Explanation);

/// <summary>
/// Converts observed Android state into conservative, evidence-backed capability hints.
/// This analyzer never attempts an exploit, unlock, flash, or other state-changing action.
/// </summary>
public sealed class AndroidCapabilityAnalyzer
{
    public IReadOnlyList<AndroidCapability> Analyze(AndroidDevice device)
    {
        var result = new List<AndroidCapability>();
        var props = device.Properties;
        var adbOnline = device.State == DeviceConnectionState.Connected;

        result.Add(new("ADB shell", adbOnline ? CapabilityStatus.Available : CapabilityStatus.Unavailable,
            $"ADB connection state: {device.State}", "An online ADB connection permits read-only shell inspection."));

        result.Add(new("USB debugging authorization",
            device.State == DeviceConnectionState.Unauthorized ? CapabilityStatus.Unavailable : adbOnline ? CapabilityStatus.Available : CapabilityStatus.Unknown,
            $"ADB connection state: {device.State}", "The device must authorize this computer before normal ADB inspection is available."));

        AddPropertyCapability(result, props, "Verified Boot state", "ro.boot.verifiedbootstate",
            "The reported Verified Boot state is evidence about the current boot chain, not proof that a rooting path exists.");
        AddPropertyCapability(result, props, "Bootloader lock state", "ro.boot.flash.locked",
            "A reported lock state helps determine which documented recovery or bootloader paths may be relevant.");
        AddPropertyCapability(result, props, "A/B slot state", "ro.boot.slot_suffix",
            "Slot information is useful when analyzing modern Android boot and recovery layouts.");

        var manufacturer = Get(props, "ro.product.manufacturer");
        var model = Get(props, "ro.product.model");
        var build = Get(props, "ro.build.id");
        result.Add(new("Device fingerprint available",
            string.IsNullOrWhiteSpace(manufacturer) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(build)
                ? CapabilityStatus.Unknown : CapabilityStatus.Available,
            $"manufacturer={Display(manufacturer)}, model={Display(model)}, build={Display(build)}",
            "A precise model/build fingerprint is the starting point for matching device-specific recovery and repair knowledge."));

        return result;
    }

    private static void AddPropertyCapability(List<AndroidCapability> result, IReadOnlyDictionary<string, string> props, string name, string key, string explanation)
    {
        var value = Get(props, key);
        result.Add(new(name, string.IsNullOrWhiteSpace(value) ? CapabilityStatus.Unknown : CapabilityStatus.Available,
            $"{key}={Display(value)}", explanation));
    }

    private static string Get(IReadOnlyDictionary<string, string> props, string key) => props.TryGetValue(key, out var value) ? value : string.Empty;
    private static string Display(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;
}
