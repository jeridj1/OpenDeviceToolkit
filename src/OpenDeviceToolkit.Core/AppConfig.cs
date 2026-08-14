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
/// Configuration for voice interaction.
/// </summary>
public sealed class VoiceConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;
    
    [JsonPropertyName("rate")]
    public int Rate { get; set; } = 0;
    
    [JsonPropertyName("volume")]
    public int Volume { get; set; } = 100;
}

/// <summary>
/// Configuration for research engine.
/// </summary>
public sealed class ResearchConfig
{
    [JsonPropertyName("enableOnlineSearch")]
    public bool EnableOnlineSearch { get; set; } = true;
    
    [JsonPropertyName("maxSearchResults")]
    public int MaxSearchResults { get; set; } = 10;
    
    [JsonPropertyName("searchTimeoutSeconds")]
    public int SearchTimeoutSeconds { get; set; } = 30;
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
    
    [JsonPropertyName("voice")]
    public VoiceConfig Voice { get; set; } = new();
    
    [JsonPropertyName("research")]
    public ResearchConfig Research { get; set; } = new();

    public static AppConfig Load()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(configPath))
        {
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
