namespace OpenDeviceToolkit.Hardware.Rp2040;

public abstract class Rp2040ControllerBase : IRp2040Controller
{
    public abstract Rp2040Mode CurrentMode { get; protected set; }
    public abstract bool IsConnected { get; protected set; }
    public abstract IReadOnlyList<Rp2040Mode> AvailableModes { get; }
    public abstract Task<bool> ConnectAsync(CancellationToken ct = default);
    public abstract Task DisconnectAsync(CancellationToken ct = default);
    public abstract Task<bool> SwitchModeAsync(Rp2040Mode mode, CancellationToken ct = default);
    public abstract Task<string> ExecuteCommandAsync(string command, CancellationToken ct = default);
    public abstract Task<byte[]> ReadAsync(int count, CancellationToken ct = default);
    public abstract Task<int> WriteAsync(byte[] data, CancellationToken ct = default);
    public abstract Task<LogicCapture> CaptureLogicAsync(IReadOnlyList<int> pins, TimeSpan duration, int sampleRateHz, CancellationToken ct = default);
    public abstract Task<double?> MeasureVoltageAsync(int pin, CancellationToken ct = default);
    public abstract Task<bool> ConfigurePinsAsync(IReadOnlyList<Rp2040PinConfig> configs, CancellationToken ct = default);
    public abstract void Dispose();
}
