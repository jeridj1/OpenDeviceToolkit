namespace OpenDeviceToolkit.Core.Speech;

public enum VoiceCommandType
{
    Unknown,
    ScanDevice,
    GainAccess,
    GenerateReport,
    RebootDevice,
    CustomObjective,
    Help,
    Exit
}

public sealed record VoiceCommand
{
    public VoiceCommandType Type { get; init; } = VoiceCommandType.Unknown;
    public string? Device { get; init; }
    public string? Objective { get; init; }
}
