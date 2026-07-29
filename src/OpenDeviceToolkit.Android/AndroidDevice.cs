namespace OpenDeviceToolkit.Android;

public enum DeviceConnectionState
{
    Unknown,
    Unauthorized,
    Offline,
    Connected
}

public sealed record AndroidDevice(
    string Serial,
    DeviceConnectionState State,
    IReadOnlyDictionary<string, string> Properties)
{
    public string Model => Get("ro.product.model");
    public string Manufacturer => Get("ro.product.manufacturer");
    public string AndroidVersion => Get("ro.build.version.release");
    public string SecurityPatch => Get("ro.build.version.security_patch");
    public string BuildId => Get("ro.build.id");
    public string SoftwareVersion => Get("ro.lge.swversion");
    public string Platform => Get("ro.board.platform");
    public string Hardware => Get("ro.hardware");
    public string BootState => Get("ro.boot.verifiedbootstate");
    public string FlashLocked => Get("ro.boot.flash.locked");
    public string Slot => Get("ro.boot.slot_suffix");

    public string Get(string key) => Properties.TryGetValue(key, out var value) ? value : string.Empty;
}
