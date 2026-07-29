using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Tests;

public sealed class OperationGuardTests
{
    [Fact]
    public void DestructiveOperationRequiresExplicitConfirmation()
    {
        var operation = new DeviceOperation("test.flash", "Flash", "Test", OperationRisk.Destructive, true);
        var context = new OperationContext("ABC123");

        Assert.Throws<DeviceOperationException>(() => DeviceOperationGuard.Validate(operation, context));
    }

    [Fact]
    public void ConfirmedDestructiveOperationIsAllowed()
    {
        var operation = new DeviceOperation("test.flash", "Flash", "Test", OperationRisk.Destructive, true);
        var context = new OperationContext("ABC123", true);

        DeviceOperationGuard.Validate(operation, context);
    }

    [Fact]
    public void EveryOperationRequiresATargetSerial()
    {
        var operation = new DeviceOperation("test.read", "Read", "Test", OperationRisk.ReadOnly);

        Assert.Throws<DeviceOperationException>(() => DeviceOperationGuard.Validate(operation, new OperationContext("")));
    }
}
