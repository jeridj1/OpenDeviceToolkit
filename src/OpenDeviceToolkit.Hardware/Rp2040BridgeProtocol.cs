namespace OpenDeviceToolkit.Hardware;

public sealed record Rp2040PinAssignment(int Gpio, string Function, bool DriveEnabled, string? VoltageDomain = null);
public sealed record Rp2040BridgeConfiguration(ProbeMode Mode, int SampleRateHz, int? BaudRate, IReadOnlyList<Rp2040PinAssignment> Pins);

public static class Rp2040BridgeProtocol
{
    public const int ProtocolVersion = 1;

    public static Rp2040BridgeConfiguration SafeSniffer(IEnumerable<int> gpios, int sampleRateHz = 1_000_000) =>
        new(ProbeMode.HighImpedanceSniffer, sampleRateHz, null, gpios.Select(g => new Rp2040PinAssignment(g, "input", false)).ToArray());

    public static Rp2040BridgeConfiguration Uart(int rx, int tx, int baudRate) =>
        new(ProbeMode.UartBridge, 0, baudRate, [new(rx, "RX", false), new(tx, "TX", true)]);

    public static Rp2040BridgeConfiguration Swd(int swdio, int swclk, int? reset = null) =>
        new(ProbeMode.SwdDebugger, 0, null, reset is null
            ? [new(swdio, "SWDIO", true), new(swclk, "SWCLK", true)]
            : [new(swdio, "SWDIO", true), new(swclk, "SWCLK", true), new(reset.Value, "RESET", true)]);
}
