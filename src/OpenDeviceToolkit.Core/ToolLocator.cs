namespace OpenDeviceToolkit.Core.Tools;

public sealed record ToolInfo(string Name, string? Path, bool Found);

public sealed class ToolLocator
{
    private readonly Workspace _workspace;

    public ToolLocator(Workspace workspace) => _workspace = workspace;

    /// <summary>
    /// Find an executable by name. Prefers workspace Tools directories, then the system PATH.
    /// On Windows this will try the ".exe" suffix as well.
    /// </summary>
    public ToolInfo Find(string executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
            throw new ArgumentException("executableName is required", nameof(executableName));

        // On Windows, consider common executable suffixes.
        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        var candidates = isWindows
            ? new[] { executableName, executableName + ".exe" }
            : new[] { executableName };

        var localCandidates = new[]
        {
            // top-level Tools\<candidate>
            () => Path.Combine(_workspace.Tools, candidates[0]),
            // platform-tools subdir
            () => Path.Combine(_workspace.Tools, "platform-tools", candidates[0]),
            // adb subdir
            () => Path.Combine(_workspace.Tools, "adb", candidates[0])
        };

        // Check each candidate with optional suffixes
        foreach (var getPath in localCandidates)
        {
            foreach (var candidate in candidates)
            {
                var path = getPath().Replace(candidates[0], candidate);
                try
                {
                    if (File.Exists(path))
                        return new ToolInfo(executableName, path, true);
                }
                catch
                {
                    // ignore and continue
                }
            }
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                foreach (var candidate in candidates)
                {
                    var candidatePath = Path.Combine(directory.Trim(), candidate);
                    if (File.Exists(candidatePath))
                        return new ToolInfo(executableName, candidatePath, true);
                }
            }
            catch (ArgumentException)
            {
                // Ignore malformed PATH entries.
            }
        }

        return new ToolInfo(executableName, null, false);
    }
}
