using OpenDeviceToolkit.Android;

namespace OpenDeviceToolkit.Tests;

public sealed class FastbootManagerTests
{
    [Fact]
    public void ParseDevices_ParsesFastbootDevices()
    {
        const string text = "List of devices attached\nABC123\tfastboot\nDEF456\tfastboot\n";

        var devices = FastbootManager.ParseDevices(text);

        Assert.Equal(2, devices.Count);
        Assert.Equal("ABC123", devices[0].Serial);
        Assert.Equal(FastbootConnectionState.Fastboot, devices[0].State);
        Assert.Equal("DEF456", devices[1].Serial);
        Assert.Equal(FastbootConnectionState.Fastboot, devices[1].State);
    }

    [Fact]
    public void ParseDevices_MapsUnknownState()
    {
        const string text = "X1\tfastboot\nY2\tdownload\n";

        var devices = FastbootManager.ParseDevices(text);

        Assert.Equal(2, devices.Count);
        Assert.Equal(FastbootConnectionState.Fastboot, devices[0].State);
        Assert.Equal(FastbootConnectionState.Unknown, devices[1].State);
    }

    [Fact]
    public void ParseDevices_IgnoresHeaderAndBlankLines()
    {
        const string text = "List of devices attached\n\nonlyonepart\n";

        var devices = FastbootManager.ParseDevices(text);

        Assert.Empty(devices);
    }

    [Fact]
    public void ParseGetVar_ParsesVariablesAndSkipsInfoLines()
    {
        const string text = "product: Pixel\nbattery-soc-ok: yes\nFinished. Total time: 1.234s\nFAILED (command failed)\n";

        var vars = FastbootManager.ParseGetVar(text);

        Assert.Equal(2, vars.Count);
        Assert.Equal("Pixel", vars["product"]);
        Assert.Equal("yes", vars["battery-soc-ok"]);
    }

    [Fact]
    public void PlanFor_FlashIsPersistentWrite()
    {
        var plan = FastbootOperationService.PlanFor("flash boot", OperationRisk.PersistentWrite, ready: true, "Artifact verified.");

        Assert.Equal("flash boot", plan.Name);
        Assert.Equal(OperationRisk.PersistentWrite, plan.Risk);
        Assert.True(plan.Ready);
        Assert.Contains("Verified target artifact and checksum", plan.Preconditions);
    }

    [Fact]
    public void PlanFor_GetVarIsReadOnly()
    {
        var plan = FastbootOperationService.PlanFor("getvar product", OperationRisk.ReadOnly, ready: true, "Ready.");

        Assert.Equal(OperationRisk.ReadOnly, plan.Risk);
        Assert.DoesNotContain("Explicit user confirmation", plan.Preconditions);
    }

    [Fact]
    public void Guard_BlocksUnconfirmedFlash()
    {
        var plan = FastbootOperationService.PlanFor("flash boot", OperationRisk.PersistentWrite, ready: true, "Artifact verified.");
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(plan, "ABC123", explicitlyConfirmed: false));
    }

    [Fact]
    public void Guard_AllowsConfirmedVerifiedFlash()
    {
        var plan = FastbootOperationService.PlanFor("flash boot", OperationRisk.PersistentWrite, ready: true, "Artifact verified.");
        OperationGuard.Require(plan, "ABC123", explicitlyConfirmed: true);
    }

    [Fact]
    public void Guard_BlocksFlashWhenArtifactNotVerified()
    {
        var plan = FastbootOperationService.PlanFor("flash boot", OperationRisk.PersistentWrite, ready: false, "Artifact not verified.");
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(plan, "ABC123", explicitlyConfirmed: true));
    }

    [Fact]
    public void Guard_BlocksEraseWithoutConfirmation()
    {
        var plan = FastbootOperationService.PlanFor("erase userdata", OperationRisk.PersistentWrite, ready: true, "Ready.");
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(plan, "ABC123", explicitlyConfirmed: false));
    }

    [Fact]
    public void Guard_BlocksRebootWithoutConfirmation()
    {
        var plan = FastbootOperationService.PlanFor("reboot", OperationRisk.StateChange, ready: true, "Ready.");
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(plan, "ABC123", explicitlyConfirmed: false));
    }
}
