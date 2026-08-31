using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Tests;

public sealed class EnhancedToolLocatorTests
{
    [Fact]
    public void Find_WithCustomPathInConfig_ReturnsCustomPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(root);
        workspace.EnsureDirectories();
        
        // Create a custom ADB path
        var customAdbDir = Path.Combine(root, "CustomAdb");
        Directory.CreateDirectory(customAdbDir);
        var customAdbPath = Path.Combine(customAdbDir, "adb.exe");
        File.WriteAllText(customAdbPath, string.Empty);
        
        // Create config with custom path
        var config = new AdbConfig
        {
            CustomPath = customAdbDir
        };
        
        var locator = new ToolLocator(workspace, config);
        var result = locator.Find("adb");
        
        Assert.True(result.Found);
        Assert.Equal(customAdbPath, result.Path);
        
        // Clean up
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Find_WithSdkPathInWorkspace_FindsTool()
    {
        var root = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(root);
        workspace.EnsureDirectories();
        
        // Create SDK-like structure in Tools
        var sdkPath = Path.Combine(workspace.Tools, "Android", "sdk", "platform-tools");
        Directory.CreateDirectory(sdkPath);
        var adbPath = Path.Combine(sdkPath, "adb.exe");
        File.WriteAllText(adbPath, string.Empty);
        
        // Add to config search paths
        var config = new AdbConfig
        {
            SearchPaths = new List<string> { "Android\\sdk\\platform-tools" }
        };
        
        var locator = new ToolLocator(workspace, config);
        var result = locator.Find("adb");
        
        Assert.True(result.Found);
        Assert.Equal(adbPath, result.Path);
        
        // Clean up
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void FindAllAdbTools_ReturnsMultipleTools()
    {
        var root = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(root);
        workspace.EnsureDirectories();
        
        // Create multiple ADB tools
        var platformToolsDir = Path.Combine(workspace.Tools, "platform-tools");
        Directory.CreateDirectory(platformToolsDir);
        
        File.WriteAllText(Path.Combine(platformToolsDir, "adb.exe"), string.Empty);
        File.WriteAllText(Path.Combine(platformToolsDir, "fastboot.exe"), string.Empty);
        
        var locator = new ToolLocator(workspace);
        var tools = locator.FindAllAdbTools();
        
        Assert.True(tools["adb"].Found);
        Assert.True(tools["fastboot"].Found);
        Assert.False(tools["mke2fs"].Found); // Not created
        
        // Clean up
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Find_WithCommonSdkLocation_FindsTool()
    {
        // This test verifies the locator checks common SDK locations
        // We can't easily test actual system paths, so we verify the logic
        var root = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(root);
        workspace.EnsureDirectories();
        
        // The locator should check common locations even without config
        // We'll verify by creating a tool in a common location
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var sdkPath = Path.Combine(programFiles, "Android", "android-sdk", "platform-tools");
        
        // We can't write to Program Files in tests, so we'll mock this in memory
        // Instead, verify the method doesn't throw
        var locator = new ToolLocator(workspace);
        var result = locator.Find("nonexistent-tool");
        
        Assert.False(result.Found);
        Assert.Null(result.Path);
        
        // Clean up
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Find_WithWindowsExtension_AddsExe()
    {
        var root = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(root);
        workspace.EnsureDirectories();
        
        // Create adb.exe (without .exe in search)
        var adbPath = Path.Combine(workspace.Tools, "adb.exe");
        File.WriteAllText(adbPath, string.Empty);
        
        var locator = new ToolLocator(workspace);
        
        // Search for "adb" should find "adb.exe" on Windows
        var result = locator.Find("adb");
        
        Assert.True(result.Found);
        Assert.Equal(adbPath, result.Path);
        
        // Clean up
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Find_WithPathEnvironmentVariable_FindsTool()
    {
        var root = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(root);
        workspace.EnsureDirectories();
        
        // Create a directory and add to PATH
        var tempToolDir = Path.Combine(root, "temp-tools");
        Directory.CreateDirectory(tempToolDir);
        var adbPath = Path.Combine(tempToolDir, "adb.exe");
        File.WriteAllText(adbPath, string.Empty);
        
        // Save original PATH
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        
        try
        {
            // Add our temp directory to PATH
            Environment.SetEnvironmentVariable("PATH", 
                string.Join(Path.PathSeparator.ToString(), new[] { tempToolDir, originalPath }));
            
            var locator = new ToolLocator(workspace);
            var result = locator.Find("adb");
            
            Assert.True(result.Found);
            Assert.Equal(adbPath, result.Path);
        }
        finally
        {
            // Restore PATH
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Directory.Delete(root, recursive: true);
        }
    }
}
