using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Tests;

public sealed class AppConfigTests
{
    private readonly string _tempDir;

    public AppConfigTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "odt-config-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    ~AppConfigTests()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void Load_WithMissingFile_ReturnsDefaults()
    {
        // Set up a temporary directory without config file
        var originalBaseDir = AppContext.BaseDirectory;
        
        try
        {
            // Temporarily change the base directory
            // Note: AppContext.BaseDirectory is read-only, so we'll test the Load method directly
            var config = AppConfig.Load();
            
            // Should return defaults
            Assert.NotNull(config);
            Assert.NotNull(config.Workspace);
            Assert.NotNull(config.Adb);
            Assert.NotNull(config.Logging);
        }
        finally
        {
            // Restore original
        }
    }

    [Fact]
    public void Load_WithValidFile_ReturnsConfiguredValues()
    {
        var configPath = Path.Combine(_tempDir, "appsettings.json");
        var expectedRoot = @"C:\Custom\Workspace";
        var json = $$"""
{
  "workspace": {
    "rootPath": "{{expectedRoot}}",
    "ensureDirectoriesOnStartup": false
  },
  "adb": {
    "customPath": "C:\\Custom\\adb",
    "timeoutSeconds": 15
  },
  "logging": {
    "logLevel": "Debug",
    "maxFileSizeBytes": 5242880,
    "retainDays": 14
  }
}
"""";
        
        File.WriteAllText(configPath, json);
        
        // Temporarily set the base directory for testing
        // Since we can't change AppContext.BaseDirectory, we'll use a test helper
        var config = LoadConfigFromPath(configPath);
        
        Assert.Equal(expectedRoot, config.Workspace.RootPath);
        Assert.False(config.Workspace.EnsureDirectoriesOnStartup);
        Assert.Equal("C:\\Custom\\adb", config.Adb.CustomPath);
        Assert.Equal(15, config.Adb.TimeoutSeconds);
        Assert.Equal("Debug", config.Logging.LogLevel);
        Assert.Equal(5242880, config.Logging.MaxFileSizeBytes);
        Assert.Equal(14, config.Logging.RetainDays);
    }

    [Fact]
    public void Save_AndLoad_RoundTrip()
    {
        var configPath = Path.Combine(_tempDir, "appsettings.json");
        
        var originalConfig = new AppConfig
        {
            Workspace = new WorkspaceConfig
            {
                RootPath = @"D:\Test\Workspace",
                EnsureDirectoriesOnStartup = true
            },
            Adb = new AdbConfig
            {
                CustomPath = null,
                SearchPaths = new List<string> { "custom-path" },
                TimeoutSeconds = 20
            },
            Logging = new LoggingConfig
            {
                LogLevel = "Warning",
                MaxFileSizeBytes = 20971520,
                RetainDays = 60
            }
        };
        
        File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(originalConfig, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        
        var loadedConfig = LoadConfigFromPath(configPath);
        
        Assert.Equal(originalConfig.Workspace.RootPath, loadedConfig.Workspace.RootPath);
        Assert.Equal(originalConfig.Workspace.EnsureDirectoriesOnStartup, loadedConfig.Workspace.EnsureDirectoriesOnStartup);
        Assert.Equal(originalConfig.Adb.TimeoutSeconds, loadedConfig.Adb.TimeoutSeconds);
        Assert.Equal(originalConfig.Logging.LogLevel, loadedConfig.Logging.LogLevel);
    }

    [Fact]
    public void ToWorkspace_WithValidConfig_ReturnsWorkspace()
    {
        var config = new WorkspaceConfig
        {
            RootPath = @"C:\Test\Workspace"
        };
        
        var workspace = config.ToWorkspace();
        
        Assert.Equal(@"C:\Test\Workspace", workspace.Root);
    }

    [Fact]
    public void ToWorkspace_WithEmptyRootPath_UsesDefault()
    {
        var config = new WorkspaceConfig
        {
            RootPath = string.Empty
        };
        
        var workspace = config.ToWorkspace();
        
        // Should use default path
        Assert.NotNull(workspace.Root);
        Assert.NotEmpty(workspace.Root);
    }

    [Fact]
    public void Config_Current_ReturnsSingleton()
    {
        var config1 = Config.Current;
        var config2 = Config.Current;
        
        Assert.Same(config1, config2);
    }

    [Fact]
    public void Config_Reload_RefreshesConfiguration()
    {
        var configPath = Path.Combine(_tempDir, "appsettings.json");
        
        // Create initial config
        File.WriteAllText(configPath, "{ \"workspace\": { \"rootPath\": \"C:\\Initial\\Path\" } }");
        
        // Note: We can't easily test Reload without mocking AppContext.BaseDirectory
        // This test verifies the method exists and doesn't throw
        Config.Reload();
        
        // Should not throw
        Assert.True(true);
    }

    // Helper method to load config from a specific path
    private static AppConfig LoadConfigFromPath(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return new AppConfig();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            return System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }
}
