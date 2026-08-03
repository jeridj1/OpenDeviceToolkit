namespace OpenDeviceToolkit.Android;

public enum CapabilityStatus
{
    Available,
    Unavailable,
    Unknown,
    RequiresPrivilege
}

public sealed record AndroidCapability(
    string Name,
    CapabilityStatus Status,
    string Evidence,
    string Explanation);

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

        result.Add(new(
            "ADB shell",
            device.State == DeviceConnectionState.Connected ? CapabilityStatus.Available : CapabilityStatus.Unavailable,
            $"ADB connection state: {device.State}",
            "An online ADB connection permits read-only shell inspection."));

        result.Add(new(
            "USB debugging authorization",
            device.State == DeviceConnectionState.Unauthorized ? CapabilityStatus.Unavailable : CapabilityStatus.Available,
            $"ADB connection state: {device.State}",
            "The device must authorize this computer before normal ADB inspection is available."));

        var verifiedBoot = Get(props, "ro.boot.verifiedbootstate");
        result.Add(new(
            "Verified Boot state known",
            string.IsNullOrWhiteSpace(verifiedBoot) ? CapabilityStatus.Unknown : CapabilityStatus.Available,
            $"ro.boot.verifiedbootstate={Display(verifiedBoot)}",
            "The reported Verified Boot state is evidence about the current boot chain, not proof that a rooting path exists."));

        var flashLocked = Get(props, "ro.boot.flash.locked");
        result.Add(new(
            "Bootloader lock state known",
            string.IsNullOrWhiteSpace(flashLocked) ? CapabilityStatus.Unknown : CapabilityStatus.Available,
            $"ro.boot.flash.locked={Display(flashLocked)}",
            "A reported lock state helps determine which documented recovery or bootloader paths may be relevant."));

        var slot = Get(props, "ro.boot.slot_suffix");
        result.Add(new(
            "A/B slot state known",
            string.IsNullOrWhiteSpace(slot) ? CapabilityStatus.Unknown : CapabilityStatus.Available,
            $"ro.boot.slot_suffix={Display(slot)}",
            "Slot information is useful when analyzing modern Android boot and recovery layouts."));

        var manufacturer = Get(props, "ro.product.manufacturer");
        var model = Get(props, "ro.product.model");
        var build = Get(props, "ro.build.id");
        result.Add(new(
            "Device fingerprint available",
            string.IsNullOrWhiteSpace(manufacturer) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(build)
                ? CapabilityStatus.Unknown
                : CapabilityStatus.Available,
            $"manufacturer={Display(manufacturer)}, model={Display(model)}, build={Display(build)}",
            "A precise model/build fingerprint is the starting point for matching device-specific recovery and repair knowledge."));

        return result;
    }

    private static string Get(IReadOnlyDictionary<string, string> props, string key) =>
        props.TryGetValue(key, out var value) ? value : string.Empty;

    private static string Display(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;
}
