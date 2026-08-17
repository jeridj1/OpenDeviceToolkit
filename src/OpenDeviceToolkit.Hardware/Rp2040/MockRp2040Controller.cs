namespace OpenDeviceToolkit.Hardware.Rp2040;

public sealed class MockRp2040Controller : Rp2040ControllerBase
{
    private Rp2040Mode _currentMode = Rp2040Mode.Gpio;
    private bool _isConnected = true;
    public override Rp2040Mode CurrentMode { get => _currentMode; protected set => _currentMode = value; }
    public override bool IsConnected { get => _isConnected; protected set => _isConnected = value; }
    public override IReadOnlyList<Rp2040Mode> AvailableModes => Enum.GetValues(typeof(Rp2040Mode)).Cast<Rp2040Mode>().ToList().AsReadOnly();
    public override Task<bool> ConnectAsync(CancellationToken ct = default) { IsConnected = true; return Task.FromResult(true); }
    public override Task DisconnectAsync(CancellationToken ct = default) { IsConnected = false; return Task.CompletedTask; }
    public override Task<bool> SwitchModeAsync(Rp2040Mode mode, CancellationToken ct = default) { CurrentMode = mode; return Task.FromResult(true); }
    public override Task<string> ExecuteCommandAsync(string command, CancellationToken ct = default) => Task.FromResult($"MOCK: {command}");
    public override Task<byte[]> ReadAsync(int count, CancellationToken ct = default) => Task.FromResult(new byte[count]);
    public override Task<int> WriteAsync(byte[] data, CancellationToken ct = default) => Task.FromResult(data.Length);
    public override Task<LogicCapture> CaptureLogicAsync(IReadOnlyList<int> pins, TimeSpan duration, int sampleRateHz, CancellationToken ct = default)
    {
        var samples = new List<LogicSample>();
        var end = DateTime.UtcNow + duration;
        var interval = TimeSpan.FromSeconds(1.0 / sampleRateHz);
        while (DateTime.UtcNow < end) { samples.Add(new LogicSample(DateTime.UtcNow - DateTime.UtcNow, pins.Select(_ => false).ToList().AsReadOnly())); Thread.Sleep(interval); }
        return Task.FromResult(new LogicCapture(DateTime.UtcNow, duration, sampleRateHz, pins.ToList().AsReadOnly(), samples.AsReadOnly()));
    }
    public override Task<double?> MeasureVoltageAsync(int pin, CancellationToken ct = default) => Task.FromResult<double?>(3.3);
    public override Task<bool> ConfigurePinsAsync(IReadOnlyList<Rp2040PinConfig> configs, CancellationToken ct = default) => Task.FromResult(true);
    public override void Dispose() { }
}
