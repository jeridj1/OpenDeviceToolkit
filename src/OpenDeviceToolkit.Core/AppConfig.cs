using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDeviceToolkit.Core;

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
            Console.Error.WriteLine($