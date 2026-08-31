namespace OpenDeviceToolkit.Core;

/// <summary>
/// Represents the file system workspace for OpenDeviceToolkit.
/// All paths are absolute and use the configured root directory.
/// </summary>
public sealed class Workspace
{
    private readonly string _root;

    /// <summary>
    /// Gets the root directory of the workspace.
    /// </summary>
    public string Root => _root;

    /// <summary>
    /// Gets the path to the Reports directory.
    /// </summary>
    public string Reports => Path.Combine(_root, "Reports");

    /// <summary>
    /// Gets the path to the Logs directory.
    /// </summary>
    public string Logs => Path.Combine(_root, "Logs");

    /// <summary>
    /// Gets the path to the Backups directory.
    /// </summary>
    public string Backups => Path.Combine(_root, "Backups");

    /// <summary>
    /// Gets the path to the Downloads directory.
    /// </summary>
    public string Downloads => Path.Combine(_root, "Downloads");

    /// <summary>
    /// Gets the path to the Firmware directory.
    /// </summary>
    public string Firmware => Path.Combine(_root, "Firmware");

    /// <summary>
    /// Gets the path to the Drivers directory.
    /// </summary>
    public string Drivers => Path.Combine(_root, "Drivers");

    /// <summary>
    /// Gets the path to the Tools directory.
    /// </summary>
    public string Tools => Path.Combine(_root, "Tools");

    /// <summary>
    /// Gets the path to the Workspace data directory.
    /// </summary>
    public string WorkspaceData => Path.Combine(_root, "Workspace");

    /// <summary>
    /// Initializes a new instance of the <see cref="Workspace"/> class.
    /// </summary>
    /// <param name="root">The root directory path. If null or empty, uses the configured workspace path or default.</param>
    public Workspace(string? root = null)
    {
        // Try to use configured workspace path if available
        if (string.IsNullOrWhiteSpace(root))
        {
            try
            {
                root = Config.Current.Workspace.RootPath;
            }
            catch
            {
                // Configuration not available, use default
            }
        }

        // Use default if still not set
        _root = string.IsNullOrWhiteSpace(root) 
            ? GetDefaultWorkspacePath() 
            : Path.GetFullPath(root.Trim());
    }

    /// <summary>
    /// Gets the default workspace path.
    /// </summary>
    private static string GetDefaultWorkspacePath()
    {
        // Try D:\OpenDeviceToolkit first (as per original design)
        var defaultPath = Path.Combine("D:", "OpenDeviceToolkit");
        if (Directory.Exists(defaultPath))
        {
            return defaultPath;
        }

        // Fall back to user's documents directory
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, "OpenDeviceToolkit");
    }

    /// <summary>
    /// Ensures all workspace directories exist.
    /// </summary>
    public void EnsureDirectories()
    {
        var directories = new[] 
        {
            Root,
            Reports,
            Logs,
            Backups,
            Downloads,
            Firmware,
            Drivers,
            Tools,
            WorkspaceData
        };

        foreach (var directory in directories)
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                // Log error but don't fail - diagnostics should never crash the app
                Console.Error.WriteLine($"Failed to create directory {directory}: {ex.Message}");
            }
        }
    }
}
