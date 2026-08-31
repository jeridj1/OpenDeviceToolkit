using OpenDeviceToolkit.Android;

namespace OpenDeviceToolkit.Tests;

public sealed class OperationGuardTests
{
    private static PlannedOperation ReadOnlyReady() =>
        new("Read-only inspection", OperationRisk.ReadOnly, true, Array.Empty<string>(), "Ready.");

    private static PlannedOperation WriteReady() =>
        new("Flash firmware", OperationRisk.PersistentWrite, true, new[] { "Verified checksum" }, "Ready.");

    private static PlannedOperation WriteBlocked() =>
        new("Flash firmware", OperationRisk.PersistentWrite, false, new[] { "Verified checksum" }, "Bootloader locked.");

    private static PlannedOperation StateChangeReady() =>
        new("RebootSystem", OperationRisk.StateChange, true, new[] { "ADB device state must be Connected" }, "ADB is online.");

    private static PlannedOperation StateChangeBlocked() =>
        new("RebootSystem", OperationRisk.StateChange, false, new[] { "ADB device state must be Connected" }, "Device not connected.");

    [Fact]
    public void ReadOnlyOperationRequiresTargetSerial()
    {
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(ReadOnlyReady(), "", explicitlyConfirmed: false));
    }

    [Fact]
    public void ReadOnlyReadyOperationPassesWithTarget()
    {
        OperationGuard.Require(ReadOnlyReady(), "ABC123", explicitlyConfirmed: false);
    }

    [Fact]
    public void WriteOperationBlockedWhenNotReady()
    {
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(WriteBlocked(), "ABC123", explicitlyConfirmed: true));
    }

    [Fact]
    public void WriteOperationRequiresExplicitConfirmation()
    {
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(WriteReady(), "ABC123", explicitlyConfirmed: false));
    }

    [Fact]
    public void ConfirmedReadyWriteOperationPasses()
    {
        OperationGuard.Require(WriteReady(), "ABC123", explicitlyConfirmed: true);
    }

    [Fact]
    public void NullOperationIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationGuard.Require(null!, "ABC123", explicitlyConfirmed: false));
    }

    [Fact]
    public void StateChangeOperationRequiresExplicitConfirmation()
    {
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(StateChangeReady(), "ABC123", explicitlyConfirmed: false));
    }

    [Fact]
    public void ConfirmedStateChangeOperationPasses()
    {
        OperationGuard.Require(StateChangeReady(), "ABC123", explicitlyConfirmed: true);
    }

    [Fact]
    public void StateChangeOperationBlockedWhenNotReady()
    {
        Assert.Throws<OperationBlockedException>(() =>
            OperationGuard.Require(StateChangeBlocked(), "ABC123", explicitlyConfirmed: true));
    }
}
