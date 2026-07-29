namespace OpenDeviceToolkit.Core;

public sealed record ToolInfo(string Name, string? Path, bool Found);

public sealed class ToolLocator
{
    private readonly Workspace _workspace;

    public ToolLocator(Workspace workspace) => _workspace = workspace;

    public ToolInfo Find(string executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
            throw new ArgumentException("An executable name is required.", nameof(executableName));

        var names = OperatingSystem.IsWindows() && !executableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? new[] { executableName + ".exe", executableName }
            : new[] { executableName };

        foreach (var name in names)
        {
            var localCandidates = new[]
            {
                Path.Combine(_workspace.Tools, name),
                Path.Combine(_workspace.Tools, "platform-tools", name),
                Path.Combine(_workspace.Tools, "adb", name)
            };

            foreach (var candidate in localCandidates)
                if (File.Exists(candidate))
                    return new ToolInfo(executableName, candidate, true);
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in names)
            {
                try
                {
                    var candidate = Path.Combine(directory.Trim(), name);
                    if (File.Exists(candidate))
                        return new ToolInfo(executableName, candidate, true);
                }
                catch (ArgumentException)
                {
                    // Ignore malformed PATH entries.
                }
            }
        }

        return new ToolInfo(executableName, null, false);
    }
}
