using System.Text.Json;

namespace OpenDeviceToolkit.Hardware;

public sealed record Rp2040Hello(int ProtocolVersion, string FirmwareVersion, IReadOnlyList<ProbeMode> Modes);
public sealed record Rp2040ConfigureRequest(ProbeMode Mode, IReadOnlyList<Rp2040PinAssignment> Pins, int SampleRateHz = 0, int BaudRate = 0);
public sealed record Rp2040SampleBatch(long TimestampTicks, IReadOnlyList<uint> Samples);
public sealed record Rp2040Result(bool Success, string Message, string? Data = null);

public static class Rp2040BridgeMessages
{
    public static string Serialize<T>(T message) => JsonSerializer.Serialize(message);
    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json) ?? throw new InvalidDataException("Invalid RP2040 bridge message.");
}
