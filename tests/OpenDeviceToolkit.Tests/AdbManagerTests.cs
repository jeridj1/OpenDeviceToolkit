using OpenDeviceToolkit.Android;

namespace OpenDeviceToolkit.Tests;

public sealed class AdbManagerTests
{
    [Fact]
    public void ParseProperties_ParsesStandardGetPropOutput()
    {
        const string text = "[ro.product.model]: [LM-V450]\n[ro.build.version.release]: [12]\n[ro.boot.slot_suffix]: [_a]\n";

        var result = AdbManager.ParseProperties(text);

        Assert.Equal("LM-V450", result["ro.product.model"]);
        Assert.Equal("12", result["ro.build.version.release"]);
        Assert.Equal("_a", result["ro.boot.slot_suffix"]);
    }

    [Fact]
    public void ParseProperties_IgnoresMalformedLines()
    {
        const string text = "not a property\n[missing separator]\n[good.key]: [good value]\n";

        var result = AdbManager.ParseProperties(text);

        Assert.Single(result);
        Assert.Equal("good value", result["good.key"]);
    }

    [Fact]
    public void ParseProperties_LastValueWinsForDuplicateKeys()
    {
        const string text = "[duplicate]: [first]\n[duplicate]: [second]\n";

        var result = AdbManager.ParseProperties(text);

        Assert.Equal("second", result["duplicate"]);
    }
}
