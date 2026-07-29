namespace OpenDeviceToolkit.Core;

public sealed record CommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration)
{
    public bool Success => ExitCode == 0;
}
