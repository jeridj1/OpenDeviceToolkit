namespace OpenDeviceToolkit.Core;

public sealed record ToolInfo(string Name, string? Path, bool Found);

public sealed class ToolLocator
{
    private readonly Workspace _workspace;

    public ToolLocator(Workspace workspace) => _workspace = workspace;

    public ToolInfo Find(string executableName)
    {
        var localCandidates = new[]
        {
            Path.Combine(_workspace.Tools, executableName),
            Path.Combine(_workspace.Tools, "platform-tools", executableName),
            Path.Combine(_workspace.Tools, "adb", executableName)
        };

        foreach (var candidate in localCandidates)
            if (File.Exists(candidate))
                return new ToolInfo(executableName, candidate, true);

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim(), executableName);
                if (File.Exists(candidate))
                    return new ToolInfo(executableName, candidate, true);
            }
            catch (ArgumentException)
            {
                // Ignore malformed PATH entries.
            }
        }

        return new ToolInfo(executableName, null, false);
    }
}
