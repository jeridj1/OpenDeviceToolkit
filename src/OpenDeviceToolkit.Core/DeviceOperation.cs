namespace OpenDeviceToolkit.Core;

public enum OperationRisk
{
    ReadOnly,
    Reversible,
    Destructive
}

public sealed record DeviceOperation(
    string Id,
    string Name,
    string Description,
    OperationRisk Risk,
    bool RequiresExplicitConfirmation = false);

public sealed record OperationContext(
    string DeviceSerial,
    bool ExplicitlyConfirmed = false);

public sealed class DeviceOperationException : Exception
{
    public DeviceOperationException(string message) : base(message) { }
}

public static class DeviceOperationGuard
{
    public static void Validate(DeviceOperation operation, OperationContext context)
    {
        if (string.IsNullOrWhiteSpace(context.DeviceSerial))
            throw new DeviceOperationException("A target device serial is required.");

        if (operation.RequiresExplicitConfirmation && !context.ExplicitlyConfirmed)
            throw new DeviceOperationException($"Operation '{operation.Name}' requires explicit confirmation.");
    }
}
