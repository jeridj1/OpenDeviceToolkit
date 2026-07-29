namespace OpenDeviceToolkit.Core;

public interface ILogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
}
