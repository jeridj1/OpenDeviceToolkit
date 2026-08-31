namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Defines the risk levels for device operations and research actions.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Read-only operations that cannot modify the device state.
    /// </summary>
    ReadOnly,
    
    /// <summary>
    /// Operations that may temporarily modify state but can be reversed.
    /// </summary>
    Reversible,
    
    /// <summary>
    /// Operations that make persistent changes to the device.
    /// </summary>
    PersistentWrite,
    
    /// <summary>
    /// Operations that may cause permanent damage or bricking.
    /// </summary>
    PotentialBrick,
    
    /// <summary>
    /// User has explicitly authorized irreversible experiments.
    /// </summary>
    EWasteMode
}

/// <summary>
/// Extension methods for RiskLevel.
/// </summary>
public static class RiskLevelExtensions
{
    public static string GetDescription(this RiskLevel level) => level switch
    {
        RiskLevel.ReadOnly => "Safe: Read-only operation",
        RiskLevel.Reversible => "Low risk: Reversible operation",
        RiskLevel.PersistentWrite => "Medium risk: Persistent changes",
        RiskLevel.PotentialBrick => "High risk: May cause permanent damage",
        RiskLevel.EWasteMode => "E-Waste mode: Irreversible experiments authorized",
        _ => "Unknown risk"
    };
    
    public static string GetColor(this RiskLevel level) => level switch
    {
        RiskLevel.ReadOnly => "Green",
        RiskLevel.Reversible => "Blue", 
        RiskLevel.PersistentWrite => "Orange",
        RiskLevel.PotentialBrick => "Red",
        RiskLevel.EWasteMode => "DarkRed",
        _ => "Gray"
    };
    
    public static bool RequiresConfirmation(this RiskLevel level) => level >= RiskLevel.PersistentWrite;
    
    public static bool RequiresDoubleConfirmation(this RiskLevel level) => level >= RiskLevel.PotentialBrick;
}
