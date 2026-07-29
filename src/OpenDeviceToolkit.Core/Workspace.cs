namespace OpenDeviceToolkit.Core;

public sealed class Workspace
{
    public string Root { get; }
    public string Reports => Path.Combine(Root, "Reports");
    public string Logs => Path.Combine(Root, "Logs");
    public string Backups => Path.Combine(Root, "Backups");
    public string Downloads => Path.Combine(Root, "Downloads");
    public string Firmware => Path.Combine(Root, "Firmware");
    public string Drivers => Path.Combine(Root, "Drivers");
    public string Tools => Path.Combine(Root, "Tools");
    public string WorkspaceData => Path.Combine(Root, "Workspace");

    public Workspace(string? root = null)
    {
        Root = string.IsNullOrWhiteSpace(root) ? @"D:\OpenDeviceToolkit" : root;
    }

    public void EnsureDirectories()
    {
        foreach (var directory in new[] { Root, Reports, Logs, Backups, Downloads, Firmware, Drivers, Tools, WorkspaceData })
            Directory.CreateDirectory(directory);
    }
}
