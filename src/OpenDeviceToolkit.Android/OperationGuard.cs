namespace OpenDeviceToolkit.Android;

/// <summary>
/// Centralized safety gate for device operations. Reuses the existing
/// <see cref="OperationRisk"/> and <see cref="PlannedOperation"/> model so that
/// every state-changing operation funnels through one set of rules instead of
/// re-implementing target-identification and confirmation checks inline.
/// </summary>
public static class OperationGuard
{
    /// <summary>
    /// Validates that a planned operation may proceed; throws
    /// <see cref="OperationBlockedException"/> otherwise. Read-only operations require
    /// an identified target. Any non-read-only operation additionally requires the plan
    /// to be ready and explicit confirmation.
    /// </summary>
    public static void Require(PlannedOperation operation, string deviceSerial, bool explicitlyConfirmed)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (string.IsNullOrWhiteSpace(deviceSerial))
            throw new OperationBlockedException("A target device serial is required before any operation.");

        if (!operation.Ready)
            throw new OperationBlockedException($"Operation '{operation.Name}' is not ready: {operation.Reason}");

        if (operation.Risk != OperationRisk.ReadOnly && !explicitlyConfirmed)
            throw new OperationBlockedException($"Operation '{operation.Name}' modifies device state and requires explicit confirmation.");
    }
}

/// <summary>
/// Thrown when an operation cannot proceed because a safety precondition
/// (target identification, readiness, or explicit confirmation) is unmet.
/// </summary>
public sealed class OperationBlockedException : Exception
{
    public OperationBlockedException(string message) : base(message) { }
}
