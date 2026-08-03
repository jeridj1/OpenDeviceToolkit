using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Tests;

public sealed class ToolLocatorTests
{
    [Fact]
    public void Find_FindsExecutableInWorkspaceToolsDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(root);
        workspace.EnsureDirectories();
        var expected = Path.Combine(workspace.Tools, "adb.exe");
        File.WriteAllText(expected, string.Empty);

        try
        {
            var result = new ToolLocator(workspace).Find("adb");
            Assert.True(result.Found);
            Assert.Equal(expected, result.Path);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Find_ReturnsNotFoundWhenToolDoesNotExist()
    {
        var root = Path.Combine(Path.GetTempPath(), "odt-test-" + Guid.NewGuid().ToString("N"));
        var workspace = new Workspace(root);
        workspace.EnsureDirectories();

        try
        {
            var result = new ToolLocator(workspace).Find("definitely-not-a-real-tool");
            Assert.False(result.Found);
            Assert.Null(result.Path);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
