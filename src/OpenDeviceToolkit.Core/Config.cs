namespace OpenDeviceToolkit.Core;

/// <summary>
/// Provides global access to the application configuration.
/// </summary>
public static class Config
{
    private static AppConfig? _current;
    
    /// <summary>
    /// Gets the current application configuration. Loads from appsettings.json on first access.
    /// </summary>
    public static AppConfig Current => _current ??= AppConfig.Load();
    
    /// <summary>
    /// Reloads the configuration from disk.
    /// </summary>
    public static void Reload() => _current = AppConfig.Load();
    
    /// <summary>
    /// Resets the configuration cache (forces reload on next access).
    /// </summary>
    public static void Reset() => _current = null;
}
