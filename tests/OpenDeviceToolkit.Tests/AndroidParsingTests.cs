using OpenDeviceToolkit.Android;

namespace OpenDeviceToolkit.Tests;

public sealed class AndroidParsingTests
{
    [Fact]
    public void ParseProperties_ParsesValidLinesAndIgnoresNoise()
    {
        const string input = "noise\n[ro.product.model]: [LM-V450]\n[ro.build.version.release]: [12]\nmalformed\n";

        var properties = AdbManager.ParseProperties(input);

        Assert.Equal("LM-V450", properties["ro.product.model"]);
        Assert.Equal("12", properties["ro.build.version.release"]);
        Assert.Equal(2, properties.Count);
    }

    [Fact]
    public void ParseProperties_LastValueWinsForDuplicateKeys()
    {
        const string input = "[key]: [first]\n[key]: [second]\n";

        var properties = AdbManager.ParseProperties(input);

        Assert.Equal("second", properties["key"]);
    }

    [Fact]
    public void AndroidDevice_ExposesTypedStateFromProperties()
    {
        var properties = new Dictionary<string, string>
        {
            ["ro.product.model"] = "LM-V450",
            ["ro.product.manufacturer"] = "LGE",
            ["ro.build.version.release"] = "12",
            ["ro.build.version.security_patch"] = "2022-05-01",
            ["ro.build.id"] = "SKQ1.211103.001",
            ["ro.lge.swversion"] = "V450VM40a",
            ["ro.board.platform"] = "msmnile",
            ["ro.hardware"] = "flashlm",
            ["ro.boot.verifiedbootstate"] = "green",
            ["ro.boot.flash.locked"] = "1",
            ["ro.boot.slot_suffix"] = "_a"
        };

        var device = new AndroidDevice("TEST", DeviceConnectionState.Connected, properties);

        Assert.Equal("LM-V450", device.Model);
        Assert.Equal("LGE", device.Manufacturer);
        Assert.Equal("12", device.AndroidVersion);
        Assert.Equal("2022-05-01", device.SecurityPatch);
        Assert.Equal("V450VM40a", device.SoftwareVersion);
        Assert.Equal("msmnile", device.Platform);
        Assert.Equal("green", device.BootState);
        Assert.Equal("1", device.FlashLocked);
        Assert.Equal("_a", device.Slot);
    }
}
