using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDeviceToolkit.Core;

public class WorkspaceSettings
{
    public string Root { get; set; } = "D:\\OpenDeviceToolkit";
}

public class VoiceSettings
{
    public bool Enabled { get; set; } = false;
    public int Rate { get; set; } = 0;
    public int Volume { get; set; } = 100;
}

public class ResearchSettings
{
    public bool EnableOnlineSearch { get; set; } = true;
    public int MaxSearchResults { get; set; } = 10;
    public int SearchTimeoutSeconds { get; set; } = 30;
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Info";
    public int MaxFileSizeKB { get; set; } = 1024;
    public int RetentionDays { get; set; } = 30;
}

public class AppSettings
{
    [JsonPropertyName("Workspace")]
    public WorkspaceSettings Workspace { get; set; } = new();
    
    [JsonPropertyName("Voice")]
    public VoiceSettings Voice { get; set; } = new();
    
    [JsonPropertyName("Research")]
    public ResearchSettings Research { get; set; } = new();
    
    [JsonPropertyName("Logging")]
    public LoggingSettings Logging { get; set; } = new();
    
    public static AppSettings Load(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            // Create default settings file
            var defaultSettings = new AppSettings();
            File.WriteAllText(path, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
            return defaultSettings;
        }
        
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }
    
    public void Save(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
