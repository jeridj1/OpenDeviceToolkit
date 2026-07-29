using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Tests;

public sealed class ToolDiagnosticsTests
{
    [Fact]
    public void MissingTool_IsReportedWithoutLaunchingAnything()
    {
        var result = ToolVersionProbe.Probe(new ToolInfo("definitely-not-installed", null, false));

        Assert.False(result.Successful);
        Assert.Null(result.Version);
        Assert.Equal("Tool not found.", result.Error);
    }
}
