using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Tests;

public sealed class WorkspaceTests
{
    [Fact]
    public void Constructor_WithNullRoot_UsesDefaultPath()
    {
        var workspace = new Workspace(null);
        
        // Should use D:\OpenDeviceToolkit or Documents\OpenDeviceToolkit
        Assert.NotNull(workspace.Root);
        Assert.NotEmpty(workspace.Root);
    }

    [Fact]
    public void Constructor_WithEmptyRoot_UsesDefaultPath()
    {
        var workspace = new Workspace(string.Empty);
        
        Assert.NotNull(workspace.Root);
        Assert.NotEmpty(workspace.Root);
    }

    [Fact]
    public void Constructor_WithCustomRoot_UsesProvidedPath()
    {
        var customPath = @"C:\Custom\Workspace";
        var workspace = new Workspace(customPath);
        
        Assert.Equal(customPath, workspace.Root);
    }

    [Fact]
    public void Constructor_WithRelativePath_ConvertsToAbsolute()
    {
        var relativePath = @"..\test";
        var workspace = new Workspace(relativePath);
        
        Assert.StartsWith(Path.GetPathRoot(Environment.CurrentDirectory), workspace.Root);
    }

    [Fact]
    public void Properties_ReturnCorrectPaths()
    {
        var root = @"C:\Test";
        var workspace = new Workspace(root);
        
        Assert.Equal(Path.Combine(root, "Reports"), workspace.Reports);
        Assert.Equal(Path.Combine(root, "Logs"), workspace.Logs);
        Assert.Equal(Path.Combine(root, "Backups"), workspace.Backups);
        Assert.Equal(Path.Combine(root, "Downloads"), workspace.Downloads);
        Assert.Equal(Path.Combine(root, "Firmware"), workspace.Firmware);
        Assert.Equal(Path.Combine(root, "Drivers"), workspace.Drivers);
        Assert.Equal(Path.Combine(root, "Tools"), workspace.Tools);
        Assert.Equal(Path.Combine(root, "Workspace"), workspace.WorkspaceData);
    }

    [Fact]
    public void EnsureDirectories_CreatesAllDirectories()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(tempPath);
        
        try
        {
            workspace.EnsureDirectories();
            
            Assert.True(Directory.Exists(workspace.Root));
            Assert.True(Directory.Exists(workspace.Reports));
            Assert.True(Directory.Exists(workspace.Logs));
            Assert.True(Directory.Exists(workspace.Backups));
            Assert.True(Directory.Exists(workspace.Downloads));
            Assert.True(Directory.Exists(workspace.Firmware));
            Assert.True(Directory.Exists(workspace.Drivers));
            Assert.True(Directory.Exists(workspace.Tools));
            Assert.True(Directory.Exists(workspace.WorkspaceData));
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, recursive: true);
            }
        }
    }
}
