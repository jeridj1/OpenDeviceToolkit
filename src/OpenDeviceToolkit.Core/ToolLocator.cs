namespace OpenDeviceToolkit.Core;

/// <summary>
/// Represents information about a located tool.
/// </summary>
public sealed record ToolInfo(string Name, string? Path, bool Found, string? Version = null);

/// <summary>
/// Locates executable tools in the workspace, common directories, and system PATH.
/// </summary>
public sealed class ToolLocator
{
    private readonly Workspace _workspace;
    private readonly AdbConfig? _adbConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolLocator"/> class.
    /// </summary>
    public ToolLocator(Workspace workspace, AdbConfig? adbConfig = null)
    {
        _workspace = workspace;
        _adbConfig = adbConfig ?? Config.Current.Adb;
    }

    /// <summary>
    /// Finds the specified executable tool.
    /// </summary>
    /// <param name="executableName">The name of the executable (e.g., "adb", "fastboot").</param>
    /// <returns>A <see cref="ToolInfo"/> object with the tool's path and status.</returns>
    public ToolInfo Find(string executableName)
    {
        // First, check if a custom path is configured
        if (!string.IsNullOrWhiteSpace(_adbConfig?.CustomPath))
        {
            var customPath = Path.Combine(_adbConfig.CustomPath, executableName);
            if (OperatingSystem.IsWindows() && !customPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                customPath += ".exe";
            }
            if (File.Exists(customPath))
            {
                return new ToolInfo(executableName, customPath, true);
            }
        }

        // Generate candidate names (add .exe on Windows if not present)
        var names = GetExecutableNames(executableName);

        // Search in workspace Tools directory and subdirectories
        var workspaceCandidates = GetWorkspaceCandidates(executableName, names);
        foreach (var candidate in workspaceCandidates)
        {
            if (File.Exists(candidate))
            {
                return new ToolInfo(executableName, candidate, true);
            }
        }

        // Search in configured ADB search paths (relative to workspace Tools)
        var configPathCandidates = GetConfigPathCandidates(executableName, names);
        foreach (var candidate in configPathCandidates)
        {
            if (File.Exists(candidate))
            {
                return new ToolInfo(executableName, candidate, true);
            }
        }

        // Search in common Android SDK locations
        var sdkCandidates = GetSdkCandidates(executableName, names);
        foreach (var candidate in sdkCandidates)
        {
            if (File.Exists(candidate))
            {
                return new ToolInfo(executableName, candidate, true);
            }
        }

        // Search in system PATH
        var pathCandidates = GetPathCandidates(executableName, names);
        foreach (var candidate in pathCandidates)
        {
            if (File.Exists(candidate))
            {
                return new ToolInfo(executableName, candidate, true);
            }
        }

        return new ToolInfo(executableName, null, false);
    }

    /// <summary>
    /// Gets the executable names to search for (adds .exe extension on Windows).
    /// </summary>
    private string[] GetExecutableNames(string executableName)
    {
        if (OperatingSystem.IsWindows() && !executableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { executableName, executableName + ".exe" };
        }
        return new[] { executableName };
    }

    /// <summary>
    /// Generates candidate paths in the workspace Tools directory.
    /// </summary>
    private IEnumerable<string> GetWorkspaceCandidates(string executableName, string[] names)
    {
        var basePaths = new[]
        {
            _workspace.Tools,
            Path.Combine(_workspace.Tools, "platform-tools"),
            Path.Combine(_workspace.Tools, "adb"),
            Path.Combine(_workspace.Tools, "fastboot"),
            Path.Combine(_workspace.Tools, "bin")
        };

        foreach (var basePath in basePaths)
        {
            foreach (var name in names)
            {
                yield return Path.Combine(basePath, name);
            }
        }
    }

    /// <summary>
    /// Generates candidate paths from configured ADB search paths.
    /// </summary>
    private IEnumerable<string> GetConfigPathCandidates(string executableName, string[] names)
    {
        if (_adbConfig?.SearchPaths == null || _adbConfig.SearchPaths.Count == 0)
        {
            yield break;
        }

        foreach (var searchPath in _adbConfig.SearchPaths)
        {
            var fullPath = Path.Combine(_workspace.Tools, searchPath);
            if (!Directory.Exists(fullPath))
            {
                continue;
            }

            foreach (var name in names)
            {
                yield return Path.Combine(fullPath, name);
            }
        }
    }

    /// <summary>
    /// Generates candidate paths in common Android SDK locations.
    /// </summary>
    private IEnumerable<string> GetSdkCandidates(string executableName, string[] names)
    {
        // Common SDK locations on Windows
        var sdkBasePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "android-sdk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "sdk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Android", "android-sdk"),
            // Common custom locations
            "C:\\Android\\sdk",
            "D:\\Android\\sdk",
            "C:\\Program Files\\Android\\android-sdk",
            "C:\\Program Files (x86)\\Android\\android-sdk"
        };

        var sdkSubPaths = new[]
        {
            "platform-tools",
            "tools",
            "tools\\bin",
            "cmdline-tools\\latest\\bin",
            "cmdline-tools\\bin"
        };

        foreach (var basePath in sdkBasePaths)
        {
            if (!Directory.Exists(basePath))
            {
                continue;
            }

            foreach (var subPath in sdkSubPaths)
            {
                var fullPath = Path.Combine(basePath, subPath);
                if (!Directory.Exists(fullPath))
                {
                    continue;
                }

                foreach (var name in names)
                {
                    yield return Path.Combine(fullPath, name);
                }
            }
        }
    }

    /// <summary>
    /// Generates candidate paths from the system PATH environment variable.
    /// </summary>
    private IEnumerable<string> GetPathCandidates(string executableName, string[] names)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var trimmedDir = directory.Trim();
                if (string.IsNullOrEmpty(trimmedDir))
                {
                    continue;
                }

                foreach (var name in names)
                {
                    yield return Path.Combine(trimmedDir, name);
                }
            }
            catch (ArgumentException)
            {
                // Ignore malformed PATH entries
            }
        }
    }

    /// <summary>
    /// Finds all available ADB-related tools (adb, fastboot, etc.).
    /// </summary>
    public IDictionary<string, ToolInfo> FindAllAdbTools()
    {
        var tools = new Dictionary<string, ToolInfo>(StringComparer.OrdinalIgnoreCase);
        var adbTools = new[] { "adb", "fastboot", "adbd", "mke2fs", "e2fsck", "sload_fvb", "abootimg" };

        foreach (var tool in adbTools)
        {
            tools[tool] = Find(tool);
        }

        return tools;
    }
}
