namespace OpenDeviceToolkit.Core;

public sealed class AppLogger
{
    private readonly Workspace _workspace;
    private readonly object _sync = new();

    public AppLogger(Workspace workspace) => _workspace = workspace;

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        var suffix = exception is null ? string.Empty : $" | {exception.GetType().Name}: {exception.Message}";
        Write("ERROR", message + suffix);
    }

    private void Write(string level, string message)
    {
        try
        {
            _workspace.EnsureDirectories();
            var path = Path.Combine(_workspace.Logs, $"odt-{DateTime.Now:yyyyMMdd}.log");
            var line = $"{DateTime.Now:O} [{level}] {message}{Environment.NewLine}";
            lock (_sync)
                File.AppendAllText(path, line);
        }
        catch
        {
            // Diagnostics must never crash the application because logging failed.
        }
    }
}
