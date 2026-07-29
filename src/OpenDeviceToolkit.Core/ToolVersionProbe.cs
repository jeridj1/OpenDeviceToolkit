using System.Diagnostics;

namespace OpenDeviceToolkit.Core;

public sealed record ToolVersionInfo(string Name, string? Version, bool Successful, string? Error);

public static class ToolVersionProbe
{
    public static ToolVersionInfo Probe(ToolInfo tool)
    {
        if (!tool.Found || string.IsNullOrWhiteSpace(tool.Path))
            return new ToolVersionInfo(tool.Name, null, false, "Tool not found.");

        var arguments = tool.Name.Equals("adb", StringComparison.OrdinalIgnoreCase)
            ? "version"
            : tool.Name.Equals("fastboot", StringComparison.OrdinalIgnoreCase)
                ? "--version"
                : "--version";

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = tool.Path,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(5000))
            {
                try { process.Kill(true); } catch { }
                return new ToolVersionInfo(tool.Name, null, false, "Timed out after 5 seconds.");
            }

            var output = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
            var firstLine = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();

            return process.ExitCode == 0
                ? new ToolVersionInfo(tool.Name, firstLine ?? "Unknown version", true, null)
                : new ToolVersionInfo(tool.Name, firstLine, false, $"Exit code {process.ExitCode}.");
        }
        catch (Exception ex)
        {
            return new ToolVersionInfo(tool.Name, null, false, ex.Message);
        }
    }
}
