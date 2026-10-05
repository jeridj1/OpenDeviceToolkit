using OpenDeviceToolkit.Android;

namespace OpenDeviceToolkit.Tests.Android;

public class AndroidSlotAnalyzerTests
{
    private const string LgV450ByNameListing = string.Join(' ', new[]
    {
        "abl_a", "abl_b", "boot_a", "boot_b", "dtbo_a", "dtbo_b", "modem_a", "modem_b",
        "recovery_a", "recovery_b", "system_a", "system_b", "vbmeta_a", "vbmeta_b",
        "vendor_a", "vendor_b", "userdata", "misc", "persist"
    });

    private static IReadOnlyDictionary<string, string> Props(params (string Key, string Value)[] pairs)
    {
        var dictionary = new Dictionary<string, string>();
        foreach (var (key, value) in pairs)
            dictionary[key] = value;
        return dictionary;
    }

    [Fact]
    public void Analyze_LgV450Evidence_IdentifiesActiveAndFallbackSlots()
    {
        var properties = Props(("ro.boot.slot_suffix", "_a"));

        var report = AndroidSlotAnalyzer.Analyze(properties, LgV450ByNameListing);

        Assert.True(report.IsSeamlessUpdateCapable);
        Assert.Equal("a", report.ActiveSlot);
        Assert.Equal("b", report.FallbackSlot);
        Assert.Equal("_a", report.SlotSuffix);
    }

    [Fact]
    public void Analyze_NoEvidence_ReportsUnknown()
    {
        var report = AndroidSlotAnalyzer.Analyze(Props(), null);

        Assert.False(report.IsSeamlessUpdateCapable);
        Assert.Equal(string.Empty, report.ActiveSlot);
        Assert.Equal(string.Empty, report.FallbackSlot);
        Assert.Contains(report.Findings, f => f.Contains("Unknown", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_SuffixWithoutPartitionListing_FallbackUnknown()
    {
        var properties = Props(("ro.boot.slot_suffix", "_b"));

        var report = AndroidSlotAnalyzer.Analyze(properties, null);

        Assert.True(report.IsSeamlessUpdateCapable);
        Assert.Equal("b", report.ActiveSlot);
        Assert.Equal(string.Empty, report.FallbackSlot);
        Assert.Contains(report.Findings, f => f.Contains("fallback slot is Unknown", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_InvalidSuffix_ReportsUnknown()
    {
        var properties = Props(("ro.boot.slot_suffix", "_c"));

        var report = AndroidSlotAnalyzer.Analyze(properties, LgV450ByNameListing);

        Assert.Equal(string.Empty, report.ActiveSlot);
        Assert.True(report.IsSeamlessUpdateCapable);
        Assert.Contains(report.Findings, f => f.Contains("not a recognized A/B suffix", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_SlotPartitionsWithoutSuffix_CapableButActiveUnknown()
    {
        var report = AndroidSlotAnalyzer.Analyze(Props(), LgV450ByNameListing);

        Assert.True(report.IsSeamlessUpdateCapable);
        Assert.Equal(string.Empty, report.ActiveSlot);
        Assert.Equal(string.Empty, report.FallbackSlot);
    }

    [Fact]
    public void Analyze_VirtualAbEnabled_IsReported()
    {
        var properties = Props(
            ("ro.boot.slot_suffix", "_a"),
            ("ro.virtual_ab.enabled", "true"));

        var report = AndroidSlotAnalyzer.Analyze(properties, LgV450ByNameListing);

        Assert.True(report.VirtualAbEnabled);
        Assert.Contains(report.Evidence, e => e.StartsWith("ro.virtual_ab.enabled=", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_VirtualAbDisabled_IsReported()
    {
        var properties = Props(("ro.virtual_ab.enabled", "false"));

        var report = AndroidSlotAnalyzer.Analyze(properties, null);

        Assert.False(report.VirtualAbEnabled);
    }

    [Fact]
    public void Analyze_ByNameListingWithSymlinkArrows_ParsesPartitionNames()
    {
        var listing = "lrwxrwxrwx 1 root root 16 Jan 1 00:00 boot_a -> /dev/block/sde14\nlrwxrwxrwx 1 root root 16 Jan 1 00:00 boot_b -> /dev/block/sde22\nlrwxrwxrwx 1 root root 16 Jan 1 00:00 userdata -> /dev/block/sda42";

        var report = AndroidSlotAnalyzer.Analyze(Props(), listing);

        Assert.True(report.IsSeamlessUpdateCapable);
        Assert.Equal(string.Empty, report.ActiveSlot);
    }

    [Fact]
    public void Analyze_NullProperties_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AndroidSlotAnalyzer.Analyze(null!, null));
    }

    [Fact]
    public void Analyze_LsStyleListing_IgnoresMetadataTokens()
    {
        var listing = string.Join(' ', new[]
        {
            "total", "48", "lrwxrwxrwx", "root", "root", "4096", "2026-01-01", "00:00",
            "boot_a", "->", "/dev/block/boot_a", "lrwxrwxrwx", "root", "root", "4096",
            "2026-01-01", "00:00", "boot_b", "->", "/dev/block/boot_b"
        });

        var report = AndroidSlotAnalyzer.Analyze(Props(), listing);

        Assert.True(report.IsSeamlessUpdateCapable);
        Assert.Equal(string.Empty, report.ActiveSlot);
    }
}
