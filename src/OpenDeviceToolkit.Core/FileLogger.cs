namespace OpenDeviceToolkit.Core;

public sealed class FileLogger : ILogger
{
    private readonly string _path;
    private readonly object _sync = new();

    public FileLogger(Workspace workspace)
    {
        workspace.EnsureDirectories();
        _path = Path.Combine(workspace.Logs, "odt.log");
    }

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message} | {exception.GetType().Name}: {exception.Message}");

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}";
        lock (_sync)
            File.AppendAllText(_path, line);
    }
}
