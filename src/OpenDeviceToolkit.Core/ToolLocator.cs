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

        foreach (var candidate in GetLocalCandidates(executableName))
        {
            if (File.Exists(candidate))
                return new ToolInfo(executableName, candidate, true);
        }

        foreach (var candidate in GetPathCandidates(executableName))
        {
            if (File.Exists(candidate))
                return new ToolInfo(executableName, candidate, true);
        }

        return new ToolInfo(executableName, null, false);
    }

    private IEnumerable<string> GetLocalCandidates(string executableName)
    {
        var names = ExpandWindowsExecutableNames(executableName);
        var directories = new[]
        {
            _workspace.Tools,
            Path.Combine(_workspace.Tools, "platform-tools"),
            Path.Combine(_workspace.Tools, "adb")
        };

        foreach (var directory in directories)
        {
            foreach (var name in names)
                yield return Path.Combine(directory, name);
        }
    }

    private static IEnumerable<string> GetPathCandidates(string executableName)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var names = ExpandWindowsExecutableNames(executableName);

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = directory.Trim().Trim('"');
            if (trimmed.Length == 0)
                continue;

            foreach (var name in names)
            {
                try
                {
                    yield return Path.Combine(trimmed, name);
                }
                catch (ArgumentException)
                {
                    // Ignore malformed PATH entries.
                }
            }
        }
    }

    private static IEnumerable<string> ExpandWindowsExecutableNames(string executableName)
    {
        if (!OperatingSystem.IsWindows() || Path.GetExtension(executableName).Length > 0)
            return [executableName];

        var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
        return pathext.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(extension => executableName + extension.Trim());
    }
}
