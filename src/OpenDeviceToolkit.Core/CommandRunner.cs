using System.ComponentModel;
using System.Diagnostics;

namespace OpenDeviceToolkit.Core;

public sealed class CommandRunner
{
    public async Task<CommandResult> RunAsync(
        string executable,
        string arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.GetTimestamp();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                    ? Environment.CurrentDirectory
                    : workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Could not start '{executable}'.");
        }
        catch (Exception ex) when (ex is Win32Exception)
        {
            throw new FileNotFoundException($"Executable not found: {executable}", executable, ex);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var timeoutTask = Task.Delay(timeout ?? TimeSpan.FromSeconds(30), cancellationToken);
        var exitTask = process.WaitForExitAsync(cancellationToken);

        var completed = await Task.WhenAny(exitTask, timeoutTask);
        if (completed == timeoutTask && !exitTask.IsCompleted)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException($"'{executable}' did not finish within the allotted time.");
        }

        await exitTask;
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var duration = Stopwatch.GetElapsedTime(started);
        return new CommandResult(process.ExitCode, stdout, stderr, duration);
    }
}
