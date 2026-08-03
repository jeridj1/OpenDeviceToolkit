using System.Runtime.InteropServices;

namespace OpenDeviceToolkit.Core;

public enum DiagnosticStatus
{
    Pass,
    Warning,
    Fail,
    Unknown
}

public sealed record DiagnosticItem(string Name, DiagnosticStatus Status, string Value, string? Evidence = null);

public sealed class EnvironmentDiagnostics
{
    private readonly Workspace _workspace;
    private readonly ToolLocator _tools;
    private readonly CommandRunner _runner;

    public EnvironmentDiagnostics(Workspace workspace, CommandRunner runner)
    {
        _workspace = workspace;
        _tools = new ToolLocator(workspace);
        _runner = runner;
    }

    public async Task<IReadOnlyList<DiagnosticItem>> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DiagnosticItem>
        {
            new("Operating system", OperatingSystem.IsWindows() ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
                RuntimeInformation.OSDescription),
            new("Process architecture", RuntimeInformation.ProcessArchitecture == Architecture.X64 ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
                RuntimeInformation.ProcessArchitecture.ToString()),
            new(".NET runtime", DiagnosticStatus.Pass, Environment.Version.ToString())
        };

        _workspace.EnsureDirectories();
        results.Add(CheckWorkspace());
        results.Add(await CheckToolAsync("ADB", "adb", "version", cancellationToken));
        results.Add(await CheckToolAsync("Fastboot", "fastboot", "--version", cancellationToken));

        return results;
    }

    private DiagnosticItem CheckWorkspace()
    {
        try
        {
            var probe = Path.Combine(_workspace.Logs, ".write-test");
            File.WriteAllText(probe, "Open Device Toolkit workspace write test.");
            File.Delete(probe);
            return new DiagnosticItem("Workspace", DiagnosticStatus.Pass, _workspace.Root, "Read/write test succeeded.");
        }
        catch (Exception ex)
        {
            return new DiagnosticItem("Workspace", DiagnosticStatus.Fail, _workspace.Root, ex.Message);
        }
    }

    private async Task<DiagnosticItem> CheckToolAsync(
        string displayName,
        string executableName,
        string arguments,
        CancellationToken cancellationToken)
    {
        var tool = _tools.Find(executableName);
        if (!tool.Found || string.IsNullOrWhiteSpace(tool.Path))
            return new DiagnosticItem(displayName, DiagnosticStatus.Warning, "Not found", "Checked ODT Tools directories and PATH.");

        try
        {
            var result = await _runner.RunAsync(tool.Path, arguments, timeout: TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
            if (!result.Success)
                return new DiagnosticItem(displayName, DiagnosticStatus.Fail, "Found, but failed to run", result.StandardError.Trim());

            var version = FirstNonEmptyLine(result.StandardOutput + Environment.NewLine + result.StandardError);
            return new DiagnosticItem(displayName, DiagnosticStatus.Pass, version, tool.Path);
        }
        catch (Exception ex)
        {
            return new DiagnosticItem(displayName, DiagnosticStatus.Fail, "Found, but could not execute", ex.Message);
        }
    }

    private static string FirstNonEmptyLine(string text)
    {
        var line = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        return string.IsNullOrWhiteSpace(line) ? "Version command returned no output" : line;
    }
}
