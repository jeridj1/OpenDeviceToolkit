namespace OpenDeviceToolkit.Core;

public sealed record EnvironmentDiagnostic(string Name, string Value, bool Healthy);

public static class EnvironmentDiagnostics
{
    public static IReadOnlyList<EnvironmentDiagnostic> Collect(Workspace workspace, params ToolInfo[] tools)
    {
        var results = new List<EnvironmentDiagnostic>
        {
            new("OS", Environment.OSVersion.VersionString, OperatingSystem.IsWindows()),
            new("64-bit process", Environment.Is64BitProcess ? "Yes" : "No", Environment.Is64BitProcess),
            new(".NET", Environment.Version.ToString(), Environment.Version.Major >= 8),
            new("Workspace", workspace.Root, Directory.Exists(workspace.Root))
        };

        foreach (var tool in tools)
            results.Add(new($"Tool: {tool.Name}", tool.Path ?? "Not found", tool.Found));

        return results;
    }
}
