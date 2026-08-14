using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDeviceToolkit.Core;

/// <summary>
/// Configuration for workspace paths.
/// </summary>
public sealed class WorkspaceConfig
{
    [JsonPropertyName("rootPath")]
    public string RootPath { get; set; } = "D:\\OpenDeviceToolkit";
    
    /// <summary>
    /// Converts this configuration to a Workspace instance.
    /// </summary>
    public Workspace ToWorkspace() => new Workspace(RootPath);
}

/// <summary>
/// Configuration for ADB tool discovery.
/// </summary>
public sealed class AdbConfig
{
    [JsonPropertyName("customPath")]
    public string? CustomPath { get; set; }
    
    [JsonPropertyName("searchPaths")]
    public List<string> SearchPaths { get; set; } = new();
}

/// <summary>
/// Configuration for logging.
/// </summary>
public sealed class LoggingConfig
{
    [JsonPropertyName("logLevel")]
    public string LogLevel { get; set; } = "Info";
    
    [JsonPropertyName("logDirectory")]
    public string? LogDirectory { get; set; }
    
    [JsonPropertyName("maxFileSizeKB")]
    public int MaxFileSizeKB { get; set; } = 1024;
    
    [JsonPropertyName("retentionDays")]
    public int RetentionDays { get; set; } = 30;
}

/// <summary>
/// Represents the application configuration loaded from appsettings.json.
/// </summary>
public sealed class AppConfig
{
    [JsonPropertyName("workspace")]
    public WorkspaceConfig Workspace { get; set; } = new();

    [JsonPropertyName("adb")]
    public AdbConfig Adb { get; set; } = new();

    [JsonPropertyName("logging")]
    public LoggingConfig Logging { get; set; } = new();

    /// <summary>
    /// Loads configuration from appsettings.json in the executable directory.
    /// </summary>
    public static AppConfig Load()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(configPath))
        {
            // Return defaults if config file doesn't exist
            return new AppConfig();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            return JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading configuration: {ex.Message}");
            return new AppConfig();
        }
    }
    
    /// <summary>
    /// Saves the configuration to appsettings.json.
    /// </summary>
    public void Save()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        File.WriteAllText(configPath, JsonSerializer.Serialize(this, options));
    }
}
