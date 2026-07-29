using System.Text.Json;

namespace OpenDeviceToolkit.Core;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Event,
    string Message,
    IReadOnlyDictionary<string, object?>? Properties = null,
    string? Exception = null);

public sealed class AppLogger
{
    private readonly Workspace _workspace;
    private readonly object _sync = new();

    public AppLogger(Workspace workspace) => _workspace = workspace;

    public void Info(string eventName, string message, IReadOnlyDictionary<string, object?>? properties = null)
        => Write("Information", eventName, message, properties);

    public void Warning(string eventName, string message, IReadOnlyDictionary<string, object?>? properties = null)
        => Write("Warning", eventName, message, properties);

    public void Error(string eventName, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null)
        => Write("Error", eventName, message, properties, exception);

    private void Write(
        string level,
        string eventName,
        string message,
        IReadOnlyDictionary<string, object?>? properties = null,
        Exception? exception = null)
    {
        try
        {
            _workspace.EnsureDirectories();
            var entry = new LogEntry(
                DateTimeOffset.UtcNow,
                level,
                eventName,
                message,
                properties,
                exception?.ToString());

            var json = JsonSerializer.Serialize(entry);
            var path = Path.Combine(_workspace.Logs, "odt.log.jsonl");
            lock (_sync)
                File.AppendAllText(path, json + Environment.NewLine);
        }
        catch
        {
            // Logging must never break device inspection.
        }
    }
}
