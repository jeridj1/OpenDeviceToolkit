namespace OpenDeviceToolkit.Hardware.Rp2040;

public interface IRp2040Controller : IDisposable
{
    Rp2040Mode CurrentMode { get; }
    bool IsConnected { get; }
    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<bool> SwitchModeAsync(Rp2040Mode mode, CancellationToken ct = default);
    IReadOnlyList<Rp2040Mode> AvailableModes { get; }
    Task<string> ExecuteCommandAsync(string command, CancellationToken ct = default);
    Task<byte[]> ReadAsync(int count, CancellationToken ct = default);
    Task<int> WriteAsync(byte[] data, CancellationToken ct = default);
    Task<LogicCapture> CaptureLogicAsync(IReadOnlyList<int> pins, TimeSpan duration, int sampleRateHz, CancellationToken ct = default);
    Task<double?> MeasureVoltageAsync(int pin, CancellationToken ct = default);
    Task<bool> ConfigurePinsAsync(IReadOnlyList<Rp2040PinConfig> configs, CancellationToken ct = default);
}

public enum Rp2040Mode { Gpio, Uart, Spi, I2c, Swd, Jtag, CmsisDap, LogicAnalyzer, OneWire, Can }

public sealed record Rp2040PinConfig(int PinNumber, Rp2040PinMode Mode, bool PullUp = false, bool PullDown = false, bool InitialState = false);

public enum Rp2040PinMode { Input, Output, InputWithPullUp, InputWithPullDown, HighImpedance }

public sealed record LogicCapture(DateTime Timestamp, TimeSpan Duration, int SampleRateHz, IReadOnlyList<int> Pins, IReadOnlyList<LogicSample> Samples);

public sealed record LogicSample(TimeSpan Timestamp, IReadOnlyList<bool> PinStates);

public sealed record Rp2040Info(string SerialNumber, string FirmwareVersion, int ProtocolVersion, IReadOnlyList<Rp2040Mode> SupportedModes);

public class Rp2040Exception : Exception { public Rp2040Exception(string m) : base(m) { } public Rp2040Exception(string m, Exception i) : base(m, i) { } }
