namespace OpenDeviceToolkit.Android;

public enum OperationRisk
{
    ReadOnly,
    StateChange,
    PersistentWrite
}

public sealed record PlannedOperation(
    string Name,
    OperationRisk Risk,
    bool Ready,
    IReadOnlyList<string> Preconditions,
    string Reason);

/// <summary>
/// Produces conservative next-step recommendations from observed device state.
/// It does not execute operations or infer unsupported modification paths.
/// </summary>
public sealed class AndroidCapabilityPlanner
{
    public IReadOnlyList<PlannedOperation> Plan(AndroidDevice device)
    {
        var plans = new List<PlannedOperation>
        {
            new("Read-only device inspection", OperationRisk.ReadOnly,
                device.State == DeviceConnectionState.Connected,
                ["ADB device state must be Connected"],
                device.State == DeviceConnectionState.Connected ? "ADB is online." : "ADB is not online."),
            new("Collect device/build fingerprint", OperationRisk.ReadOnly,
                device.State == DeviceConnectionState.Connected,
                ["ADB device state must be Connected"],
                "Exact manufacturer, model, build, security patch, and boot properties are required before selecting device-specific procedures.")
        };

        var identityReady = !string.IsNullOrWhiteSpace(device.Manufacturer) &&
                            !string.IsNullOrWhiteSpace(device.Model) &&
                            !string.IsNullOrWhiteSpace(device.BuildId);
        plans.Add(new("Match device-specific recovery knowledge", OperationRisk.ReadOnly,
            identityReady,
            ["Manufacturer, model, and build ID must be known"],
            identityReady ? "The minimum identity fields are available for a knowledge lookup." : "The device fingerprint is incomplete."));

        var locked = device.FlashLocked.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                     device.FlashLocked.Equals("true", StringComparison.OrdinalIgnoreCase);
        plans.Add(new("Persistent boot/firmware modification", OperationRisk.PersistentWrite,
            false,
            ["Exact supported procedure", "Verified target artifact and checksum", "Device-specific prerequisites", "Recovery path", "Explicit user confirmation"],
            locked ? "Bootloader reports locked; no write operation is authorized by the planner." : "No supported write procedure has been selected by the planner."));

        return plans;
    }
}
