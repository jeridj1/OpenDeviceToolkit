namespace OpenDeviceToolkit.Core.Research;

public sealed class OfflineResearcher : ResearchSourceBase
{
    private readonly AppLogger? _logger;
    private readonly Dictionary<string, DeviceFingerprint> _knownDevices = new(StringComparer.OrdinalIgnoreCase);
    
    public override string Name => "Offline Fingerprinting";
    public override int Priority => 5;
    
    public OfflineResearcher(AppLogger? logger = null)
    {
        _logger = logger;
        _knownDevices["LG-V450"] = new DeviceFingerprint { Manufacturer = "LG", Model = "V50 ThinQ", Platform = "msmnile", Exploits = new[] { "DirtyCow", "CVE-2019-2215" }, KnownMethods = new[] { "EDL Mode" } };
        _knownDevices["Qualcomm"] = new DeviceFingerprint { Manufacturer = "Qualcomm", Model = "Snapdragon", Exploits = new[] { "CVE-2019-2215" }, KnownMethods = new[] { "EDL Mode (9008)" } };
    }
    
    public override async Task<IReadOnlyList<ResearchResult>> SearchAsync(string query, Usb.UsbDeviceInfo? deviceInfo = null, CancellationToken ct = default)
    {
        var results = new List<ResearchResult>();
        results.AddRange(GenerateHypotheses(query, deviceInfo));
        if (deviceInfo != null) results.AddRange(ResearchByDeviceInfo(deviceInfo));
        return results;
    }
    
    private IEnumerable<ResearchResult> GenerateHypotheses(string query, Usb.UsbDeviceInfo? deviceInfo)
    {
        var q = query.ToLower();
        if (q.Contains("qualcomm") || q.Contains("edl")) yield return ResearchResult.Create("Qualcomm EDL Mode (9008)", "Offline DB", "https://wiki.postmarketos.org/wiki/Qualcomm_Snapdragon_855", confidence: 0.7, estimatedRisk: RiskLevel.PotentialBrick, tags: new[] { "qualcomm", "edl" });
        if (q.Contains("unlock") || q.Contains("root")) yield return ResearchResult.Create("Fastboot OEM Unlock", "Offline DB", "https://source.android.com/docs/bootloader/unlocking_the_bootloader", confidence: 0.6, estimatedRisk: RiskLevel.PersistentWrite, tags: new[] { "fastboot" });
        if (q.Contains("dirtycow") || (deviceInfo?.Manufacturer.Contains("LG") == true)) yield return ResearchResult.Create("DirtyCow Exploit", "Offline DB", "https://dirtycow.ninja/", confidence: 0.6, estimatedRisk: RiskLevel.PotentialBrick, tags: new[] { "dirtycow" });
        if (q.Contains("root") || q.Contains("magisk")) yield return ResearchResult.Create("Magisk Root", "Offline DB", "https://github.com/topjohnwu/Magisk", confidence: 0.8, estimatedRisk: RiskLevel.PersistentWrite, tags: new[] { "magisk" });
    }
    
    private IEnumerable<ResearchResult> ResearchByDeviceInfo(Usb.UsbDeviceInfo deviceInfo)
    {
        if (_knownDevices.TryGetValue(deviceInfo.Manufacturer, out var fp) || _knownDevices.TryGetValue(deviceInfo.DisplayName, out fp))
        {
            foreach (var m in fp.KnownMethods) yield return ResearchResult.Create($"{fp.Manufacturer} {fp.Model}: {m}", "Offline DB", "", m, 0.9, RiskLevel.PersistentWrite);
            foreach (var e in fp.Exploits) yield return ResearchResult.Create($"{fp.Manufacturer} {fp.Model}: {e}", "Offline DB", $"https://cve.mitre.org/cgi-bin/cvename.cgi?name={e}", e, 0.8, RiskLevel.PotentialBrick);
        }
        if (!string.IsNullOrEmpty(deviceInfo.VidPid))
        {
            if (deviceInfo.VidPid.Contains("05C6")) yield return ResearchResult.Create("Qualcomm Device", "Offline DB", "", "VID 05C6 indicates Qualcomm. Try EDL mode.", 0.9, RiskLevel.ReadOnly);
            if (deviceInfo.VidPid.Contains("1004")) yield return ResearchResult.Create("LG Device", "Offline DB", "", "VID 1004 indicates LG device.", 0.9, RiskLevel.ReadOnly);
        }
    }
    
    public override Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);
}

public sealed class DeviceFingerprint
{
    public string Manufacturer { get; set; } = "";
    public string Model { get; set; } = "";
    public string Platform { get; set; } = "";
    public IReadOnlyList<string> Exploits { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> KnownMethods { get; set; } = Array.Empty<string>();
}
