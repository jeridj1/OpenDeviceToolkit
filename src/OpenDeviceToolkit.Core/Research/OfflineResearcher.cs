namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Performs offline research using device fingerprinting and known patterns.
/// </summary>
public sealed class OfflineResearcher : ResearchSourceBase
{
    private readonly AppLogger? _logger;
    private readonly Dictionary<string, DeviceFingerprint> _knownDevices = new(StringComparer.OrdinalIgnoreCase);
    
    public override string Name => "Offline Fingerprinting";
    public override int Priority => 5; // Lower priority than online sources
    
    public OfflineResearcher(AppLogger? logger = null)
    {
        _logger = logger;
        LoadKnownDevices();
    }
    
    private void LoadKnownDevices()
    {
        // Add known device fingerprints
        _knownDevices["LG-V450"] = new DeviceFingerprint
        {
            Manufacturer = "LG",
            Model = "V50 ThinQ",
            Platform = "msmnile",
            Chipset = "Qualcomm Snapdragon 855",
            BootloaderUnlock = BootloaderUnlockStatus.LockedByDefault,
            Exploits = new[] { "DirtyCow", "CVE-2019-2215" },
            KnownMethods = new[] { "EDL Mode (Qualcomm 9008)", "LG UP Tool", "Fastboot Unlock (if allowed)" }
        };
        
        _knownDevices["LG-V40"] = new DeviceFingerprint
        {
            Manufacturer = "LG",
            Model = "V40 ThinQ",
            Platform = "sdm845",
            Chipset = "Qualcomm Snapdragon 845",
            BootloaderUnlock = BootloaderUnlockStatus.Unlockable,
            Exploits = new[] { "DirtyCow", "CVE-2018-18074" },
            KnownMethods = new[] { "Fastboot Unlock", "EDL Mode" }
        };
        
        _knownDevices["OnePlus"] = new DeviceFingerprint
        {
            Manufacturer = "OnePlus",
            Model = "Multiple",
            Platform = "msm8998",
            Chipset = "Qualcomm Snapdragon 835",
            BootloaderUnlock = BootloaderUnlockStatus.Unlockable,
            Exploits = new[] { "Fastboot Unlock" },
            KnownMethods = new[] { "fastboot oem unlock" }
        };
        
        _knownDevices["Google-Pixel"] = new DeviceFingerprint
        {
            Manufacturer = "Google",
            Model = "Pixel",
            Platform = "gs101",
            Chipset = "Google Tensor",
            BootloaderUnlock = BootloaderUnlockStatus.Unlockable,
            Exploits = new[] { "Fastboot Unlock" },
            KnownMethods = new[] { "fastboot flashing unlock" }
        };
        
        _knownDevices["Samsung"] = new DeviceFingerprint
        {
            Manufacturer = "Samsung",
            Model = "Galaxy",
            Platform = "exynos",
            Chipset = "Exynos/Snapdragon",
            BootloaderUnlock = BootloaderUnlockStatus.VariesByModel,
            Exploits = new[] { "Odin Flash", "Download Mode" },
            KnownMethods = new[] { "Odin", "Heimdall" }
        };
    }
    
    public override async Task<IReadOnlyList<ResearchResult>> SearchAsync(
        string query,
        Usb.UsbDeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ResearchResult>();
        
        try
        {
            _logger?.Info($"Performing offline research for: {query}");
            
            // Generate hypotheses based on query
            var hypotheses = GenerateHypotheses(query, deviceInfo);
            
            foreach (var hypothesis in hypotheses)
            {
                results.Add(hypothesis);
            }
            
            // If we have device info, look up known patterns
            if (deviceInfo != null)
            {
                var deviceResults = ResearchByDeviceInfo(deviceInfo);
                results.AddRange(deviceResults);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Offline research failed: {ex.Message}", ex);
        }
        
        return results;
    }
    
    private IReadOnlyList<ResearchResult> GenerateHypotheses(string query, Usb.UsbDeviceInfo? deviceInfo)
    {
        var results = new List<ResearchResult>();
        var queryLower = query.ToLower();
        
        // Hypothesis: Try EDL mode for Qualcomm devices
        if (queryLower.Contains("qualcomm") || queryLower.Contains("snapdragon") || 
            queryLower.Contains("edl") || queryLower.Contains("9008"))
        {
            results.Add(ResearchResult.Create(
                title: "Qualcomm EDL Mode (9008)",
                source: "Offline Database",
                url: "https://wiki.postmarketos.org/wiki/Qualcomm_Snapdragon_855_(msmnile)",
                snippet: "Put device in Emergency Download Mode using test points or button combinations",
                confidence: 0.7,
                estimatedRisk: RiskLevel.PotentialBrick,
                tags: new[] { "qualcomm", "edl", "9008", "msmnile" }
            ));
        }
        
        // Hypothesis: Try fastboot unlock
        if (queryLower.Contains("unlock") || queryLower.Contains("root") || queryLower.Contains("access"))
        {
            results.Add(ResearchResult.Create(
                title: "Fastboot OEM Unlock",
                source: "Offline Database",
                url: "https://source.android.com/docs/bootloader/unlocking_the_bootloader",
                snippet: "Use 'fastboot oem unlock' or 'fastboot flashing unlock' to unlock bootloader",
                confidence: 0.6,
                estimatedRisk: RiskLevel.PersistentWrite,
                tags: new[] { "fastboot", "unlock", "bootloader" }
            ));
        }
        
        // Hypothesis: Try ADB root
        if (queryLower.Contains("adb") && (queryLower.Contains("root") || queryLower.Contains("su")))
        {
            results.Add(ResearchResult.Create(
                title: "ADB Root Access",
                source: "Offline Database",
                url: "https://developer.android.com/studio/command-line/adb",
                snippet: "Try 'adb root' or 'adb shell su' if device is already rooted",
                confidence: 0.5,
                estimatedRisk: RiskLevel.Reversible,
                tags: new[] { "adb", "root", "su" }
            ));
        }
        
        // Hypothesis: Try DirtyCow exploit (for older devices)
        if (queryLower.Contains("dirtycow") || queryLower.Contains("cve-2016-5195") ||
            (deviceInfo != null && deviceInfo.Manufacturer.Contains("LG")))
        {
            results.Add(ResearchResult.Create(
                title: "DirtyCow Exploit (CVE-2016-5195)",
                source: "Offline Database",
                url: "https://dirtycow.ninja/",
                snippet: "Local privilege escalation exploit for Linux kernel < 4.8",
                confidence: 0.6,
                estimatedRisk: RiskLevel.PotentialBrick,
                tags: new[] { "dirtycow", "cve-2016-5195", "lg", "privilege escalation" }
            ));
        }
        
        // Hypothesis: Try Magisk for root
        if (queryLower.Contains("root") || queryLower.Contains("magisk"))
        {
            results.Add(ResearchResult.Create(
                title: "Magisk Root",
                source: "Offline Database",
                url: "https://github.com/topjohnwu/Magisk",
                snippet: "Systemless root solution for Android devices",
                confidence: 0.8,
                estimatedRisk: RiskLevel.PersistentWrite,
                tags: new[] { "magisk", "root", "systemless" }
            ));
        }
        
        return results;
    }
    
    private IReadOnlyList<ResearchResult> ResearchByDeviceInfo(Usb.UsbDeviceInfo deviceInfo)
    {
        var results = new List<ResearchResult>();
        
        // Try to match known devices
        if (_knownDevices.TryGetValue(deviceInfo.Manufacturer, out var fingerprint) ||
            _knownDevices.TryGetValue(deviceInfo.DisplayName, out fingerprint) ||
            _knownDevices.TryGetValue($"{deviceInfo.Manufacturer}-{deviceInfo.DisplayName}", out fingerprint))
        {
            // Add known methods
            foreach (var method in fingerprint.KnownMethods)
            {
                results.Add(ResearchResult.Create(
                    title: $"{fingerprint.Manufacturer} {fingerprint.Model}: {method}",
                    source: "Offline Database",
                    url: "",
                    snippet: method,
                    confidence: 0.9,
                    estimatedRisk: RiskLevel.PersistentWrite,
                    tags: new[] { fingerprint.Manufacturer, fingerprint.Model, "known-method" }
                ));
            }
            
            // Add known exploits
            foreach (var exploit in fingerprint.Exploits)
            {
                results.Add(ResearchResult.Create(
                    title: $"{fingerprint.Manufacturer} {fingerprint.Model}: {exploit}",
                    source: "Offline Database",
                    url: $"https://cve.mitre.org/cgi-bin/cvename.cgi?name={exploit}",
                    snippet: exploit,
                    confidence: 0.8,
                    estimatedRisk: RiskLevel.PotentialBrick,
                    tags: new[] { fingerprint.Manufacturer, fingerprint.Model, "exploit", "cve" }
                ));
            }
        }
        
        // USB VID/PID based research
        if (!string.IsNullOrEmpty(deviceInfo.VidPid))
        {
            var vidPidResults = ResearchByVidPid(deviceInfo.VidPid);
            results.AddRange(vidPidResults);
        }
        
        return results;
    }
    
    private IReadOnlyList<ResearchResult> ResearchByVidPid(string vidPid)
    {
        var results = new List<ResearchResult>();
        
        // Common VID/PID patterns
        if (vidPid.Contains("05C6")) // Qualcomm
        {
            results.Add(ResearchResult.Create(
                title: "Qualcomm Device Detected",
                source: "Offline Database",
                url: "",
                snippet: "VID 05C6 indicates Qualcomm chipset. Try EDL mode (9008) or fastboot.",
                confidence: 0.9,
                estimatedRisk: RiskLevel.ReadOnly,
                tags: new[] { "qualcomm", "05c6", "vid" }
            ));
            
            results.Add(ResearchResult.Create(
                title: "Qualcomm EDL Mode",
                source: "Offline Database",
                url: "https://wiki.postmarketos.org/wiki/Qualcomm_Snapdragon",
                snippet: "Short test points near USB port to force EDL mode",
                confidence: 0.8,
                estimatedRisk: RiskLevel.PotentialBrick,
                tags: new[] { "qualcomm", "edl", "9008" }
            ));
        }
        
        if (vidPid.Contains("1004")) // LG
        {
            results.Add(ResearchResult.Create(
                title: "LG Device Detected",
                source: "Offline Database",
                url: "",
                snippet: "VID 1004 indicates LG device. Check for LG UP Tool compatibility.",
                confidence: 0.9,
                estimatedRisk: RiskLevel.ReadOnly,
                tags: new[] { "lg", "1004", "vid" }
            ));
        }
        
        if (vidPid.Contains("18D1")) // Google
        {
            results.Add(ResearchResult.Create(
                title: "Google Device Detected",
                source: "Offline Database",
                url: "",
                snippet: "VID 18D1 indicates Google device. Bootloader is likely unlockable.",
                confidence: 0.9,
                estimatedRisk: RiskLevel.ReadOnly,
                tags: new[] { "google", "pixel", "18d1", "vid" }
            ));
        }
        
        if (vidPid.Contains("04E8")) // Samsung
        {
            results.Add(ResearchResult.Create(
                title: "Samsung Device Detected",
                source: "Offline Database",
                url: "",
                snippet: "VID 04E8 indicates Samsung device. Try Odin or Download Mode.",
                confidence: 0.9,
                estimatedRisk: RiskLevel.ReadOnly,
                tags: new[] { "samsung", "04e8", "vid" }
            ));
        }
        
        return results;
    }
    
    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Offline researcher is always available
        return true;
    }
}

/// <summary>
/// Fingerprint information for a known device.
/// </summary>
public sealed class DeviceFingerprint
{
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Chipset { get; set; } = string.Empty;
    public BootloaderUnlockStatus BootloaderUnlock { get; set; } = BootloaderUnlockStatus.Unknown;
    public IReadOnlyList<string> Exploits { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> KnownMethods { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Bootloader unlock status for a device.
/// </summary>
public enum BootloaderUnlockStatus
{
    Unknown,
    LockedByDefault,
    Unlockable,
    AlreadyUnlocked,
    VariesByModel
}
