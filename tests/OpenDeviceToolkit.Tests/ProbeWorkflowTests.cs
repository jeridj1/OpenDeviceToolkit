using OpenDeviceToolkit.Hardware;
using OpenDeviceToolkit.Hardware.Rp2040;

namespace OpenDeviceToolkit.Tests;

public sealed class ProbeWorkflowTests
{
    [Fact]
    public async Task RunAsync_ConnectedTarget_ProducesGuidanceObservationsAndCapabilities()
    {
        var workflow = new ProbeWorkflow(new MockRp2040Controller());

        var result = await workflow.RunAsync(new ProbeTarget("UART console on STM32F103"));

        Assert.Equal("UART console on STM32F103", result.TargetDescription);
        Assert.True(result.Connected);
        Assert.NotEmpty(result.ConnectionGuidance);
        Assert.NotEmpty(result.Observations);
        Assert.NotEmpty(result.Capabilities);
    }

    [Fact]
    public async Task RunAsync_KnownChip_EnablesChipSpecificInterfaces()
    {
        var workflow = new ProbeWorkflow(new MockRp2040Controller());

        var result = await workflow.RunAsync(new ProbeTarget("STM32F103"));

        Assert.Contains(result.Capabilities, c => c.Name.StartsWith("SWD", StringComparison.Ordinal));
        Assert.Contains(result.Capabilities, c => c.Name.StartsWith("UART", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_MockVoltage_EnablesDirectGpioDrive()
    {
        var workflow = new ProbeWorkflow(new MockRp2040Controller());

        var result = await workflow.RunAsync(new ProbeTarget("unknown board"));

        var drive = Assert.Single(result.Capabilities, c => c.Name.StartsWith("Direct GPIO drive", StringComparison.Ordinal));
        Assert.True(drive.Available);
    }

    [Fact]
    public void InferCapabilities_UnknownVoltage_DisablesReadonlyAndDrive()
    {
        var caps = ProbeWorkflow.InferCapabilities(voltage: null, pinout: null);

        Assert.All(caps, c => Assert.False(c.Available));
    }

    [Fact]
    public void InferCapabilities_FiveVolt_EnablesReadonlyButNotDirectDrive()
    {
        var caps = ProbeWorkflow.InferCapabilities(voltage: 5.0, pinout: null);

        Assert.True(caps.Single(c => c.Name.StartsWith("Read-only", StringComparison.Ordinal)).Available);
        Assert.False(caps.Single(c => c.Name.StartsWith("Direct GPIO drive", StringComparison.Ordinal)).Available);
    }

    [Fact]
    public void InferCapabilities_KnownPinout_AddsInterfaceCapabilities()
    {
        var pinout = PinoutDatabase.GetPinout("STM32F103");

        var caps = ProbeWorkflow.InferCapabilities(voltage: 3.3, pinout);

        Assert.NotNull(pinout);
        Assert.Contains(caps, c => c.Name.StartsWith("SWD", StringComparison.Ordinal) && c.Available);
    }
}
