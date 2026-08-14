namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Defines the risk levels for device operations and research actions.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Read-only operations that cannot modify the device state.
    /// Example: ADB getprop, fastboot devices, reading partitions.
    /// </summary>
    ReadOnly,
    
    /// <summary>
    /// Operations that may temporarily modify state but can be reversed.
    /// Example: Rebooting to bootloader, enabling ADB over network.
    /// </summary>
    Reversible,
    
    /// <summary>
    /// Operations that make persistent changes to the device.
    /// Example: Flashing recovery, unlocking bootloader, modifying system partitions.
    /// </summary>
    PersistentWrite,
    
    /// <summary>
    /// Operations that may cause permanent damage or bricking.
    /// Example: Experimental exploits, voltage glitching, writing to critical partitions.
    /// </summary>
    PotentialBrick,
    
    /// <summary>
    /// User has explicitly authorized irreversible experiments.
    /// Device is considered e-waste; bricking is acceptable.
    /// </summary>
    EWasteMode
}

/// <summary>
/// Extension methods for RiskLevel.
/// </summary>
public static class RiskLevelExtensions
{
    /// <summary>
    /// Gets a human-readable description of the risk level.
    /// </summary>
    public static string GetDescription(this RiskLevel level) => level switch
    {
        RiskLevel.ReadOnly => "Safe: Read-only operation",
        RiskLevel.Reversible => "Low risk: Reversible operation",
        RiskLevel.PersistentWrite => "Medium risk: Persistent changes",
        RiskLevel.PotentialBrick => "High risk: May cause permanent damage",
        RiskLevel.EWasteMode => "E-Waste mode: Irreversible experiments authorized",
        _ => "Unknown risk"
    };
    
    /// <summary>
    /// Gets the color associated with the risk level for UI display.
    /// </summary>
    public static string GetColor(this RiskLevel level) => level switch
    {
        RiskLevel.ReadOnly => "Green",
        RiskLevel.Reversible => "Blue", 
        RiskLevel.PersistentWrite => "Orange",
        RiskLevel.PotentialBrick => "Red",
        RiskLevel.EWasteMode => "DarkRed",
        _ => "Gray"
    };
    
    /// <summary>
    /// Checks if this risk level requires explicit user confirmation.
    /// </summary>
    public static bool RequiresConfirmation(this RiskLevel level) => level >= RiskLevel.PersistentWrite;
    
    /// <summary>
    /// Checks if this risk level requires a second confirmation for irreversible actions.
    /// </summary>
    public static bool RequiresDoubleConfirmation(this RiskLevel level) => level >= RiskLevel.PotentialBrick;
}
