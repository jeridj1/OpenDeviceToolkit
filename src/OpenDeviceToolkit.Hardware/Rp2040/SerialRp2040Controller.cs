using System.IO.Ports;

namespace OpenDeviceToolkit.Hardware.Rp2040;

public sealed class SerialRp2040Controller : Rp2040ControllerBase
{
    private SerialPort? _serialPort;
    private string? _portName;
    private int _baudRate = 115200;
    public override Rp2040Mode CurrentMode { get; protected set; } = Rp2040Mode.Gpio;
    public override bool IsConnected { get; protected set; }
    public override IReadOnlyList<Rp2040Mode> AvailableModes { get; } = new List<Rp2040Mode>
    {
        Rp2040Mode.Gpio, Rp2040Mode.Uart, Rp2040Mode.Spi, Rp2040Mode.I2c,
        Rp2040Mode.Swd, Rp2040Mode.Jtag, Rp2040Mode.CmsisDap, Rp2040Mode.LogicAnalyzer
    }.AsReadOnly();
    
    public SerialRp2040Controller(string? portName = null, int baudRate = 115200)
    {
        _portName = portName;
        _baudRate = baudRate;
    }
    
    public override async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return true;
        if (string.IsNullOrEmpty(_portName))
        {
            _portName = await AutoDetectPortAsync(ct);
            if (string.IsNullOrEmpty(_portName)) return false;
        }
        try
        {
            _serialPort = new SerialPort(_portName, _baudRate)
            {
                Handshake = Handshake.None,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                RtsEnable = true,
                DtrEnable = true
            };
            _serialPort.Open();
            IsConnected = true;
            await SendCommandAsync("PING", ct);
            return true;
        }
        catch (Exception ex)
        {
            _serialPort?.Dispose();
            _serialPort = null;
            IsConnected = false;
            throw new Rp2040Exception("Failed to connect", ex);
        }
    }
    
    public override async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_serialPort != null)
        {
            try { if (_serialPort.IsOpen) _serialPort.Close(); } catch { }
            _serialPort.Dispose();
            _serialPort = null;
        }
        IsConnected = false;
    }
    
    public override async Task<bool> SwitchModeAsync(Rp2040Mode mode, CancellationToken ct = default)
    {
        if (!AvailableModes.Contains(mode)) return false;
        if (!IsConnected) await ConnectAsync(ct);
        if (!IsConnected) return false;
        try
        {
            var r = await SendCommandAsync($"MODE:{mode}", ct);
            if (r?.StartsWith("OK") == true) { CurrentMode = mode; return true; }
            return false;
        }
        catch (Exception ex) { throw new Rp2040Exception($"Failed to switch mode", ex); }
    }
    
    public override async Task<string> ExecuteCommandAsync(string command, CancellationToken ct = default)
    {
        if (!IsConnected) await ConnectAsync(ct);
        if (!IsConnected) throw new Rp2040Exception("Not connected");
        return await SendCommandAsync(command, ct) ?? "ERROR";
    }
    
    public override async Task<byte[]> ReadAsync(int count, CancellationToken ct = default)
    {
        if (!IsConnected) await ConnectAsync(ct);
        if (!IsConnected || _serialPort == null) throw new Rp2040Exception("Not connected");
        var buffer = new byte[count];
        var bytesRead = 0;
        while (bytesRead < count && !ct.IsCancellationRequested)
        {
            var read = await _serialPort.BaseStream.ReadAsync(buffer, bytesRead, count - bytesRead, ct);
            if (read == 0) break;
            bytesRead += read;
        }
        return bytesRead < count ? buffer.AsSpan(0, bytesRead).ToArray() : buffer;
    }
    
    public override async Task<int> WriteAsync(byte[] data, CancellationToken ct = default)
    {
        if (!IsConnected) await ConnectAsync(ct);
        if (!IsConnected || _serialPort == null) throw new Rp2040Exception("Not connected");
        await _serialPort.BaseStream.WriteAsync(data, 0, data.Length, ct);
        return data.Length;
    }
    
    public override async Task<LogicCapture> CaptureLogicAsync(IReadOnlyList<int> pins, TimeSpan duration, int sampleRateHz, CancellationToken ct = default)
    {
        if (!IsConnected) await ConnectAsync(ct);
        if (!IsConnected) throw new Rp2040Exception("Not connected");
        await SwitchModeAsync(Rp2040Mode.LogicAnalyzer, ct);
        var configs = pins.Select(p => new Rp2040PinConfig(p, Rp2040PinMode.Input)).ToList();
        await ConfigurePinsAsync(configs, ct);
        var cmd = $"CAPTURE:{string.Join(",", pins)}:{sampleRateHz}:{duration.TotalMilliseconds}";
        var resp = await SendCommandAsync(cmd, ct);
        if (resp?.StartsWith("CAPTURE_OK") != true) throw new Rp2040Exception("Capture failed");
        var sampleCount = (int)(duration.TotalSeconds * sampleRateHz);
        var samples = new List<LogicSample>();
        for (int i = 0; i < sampleCount; i += 100)
        {
            if (ct.IsCancellationRequested) break;
            var chunkSize = Math.Min(100, sampleCount - i);
            var data = await ReadAsync(chunkSize * pins.Count, ct);
            for (int j = 0; j < chunkSize; j++)
            {
                var states = new List<bool>();
                for (int p = 0; p < pins.Count; p++)
                    states.Add(data.Length > j * pins.Count + p && data[j * pins.Count + p] != 0);
                samples.Add(new LogicSample(TimeSpan.FromSeconds(i / (double)sampleRateHz), states.AsReadOnly()));
            }
        }
        return new LogicCapture(DateTime.UtcNow, duration, sampleRateHz, pins.ToList().AsReadOnly(), samples.AsReadOnly());
    }
    
    public override async Task<double?> MeasureVoltageAsync(int pin, CancellationToken ct = default)
    {
        if (!IsConnected) await ConnectAsync(ct);
        if (!IsConnected) return null;
        var r = await SendCommandAsync($"ADC:{pin}", ct);
        return double.TryParse(r, out var v) ? v : null;
    }
    
    public override async Task<bool> ConfigurePinsAsync(IReadOnlyList<Rp2040PinConfig> configs, CancellationToken ct = default)
    {
        if (!IsConnected) await ConnectAsync(ct);
        if (!IsConnected) return false;
        foreach (var c in configs)
        {
            var r = await SendCommandAsync($"PIN:{c.PinNumber}:{c.Mode}:{c.PullUp}:{c.PullDown}:{c.InitialState}", ct);
            if (r?.StartsWith("OK") != true) return false;
        }
        return true;
    }
    
    private async Task<string?> SendCommandAsync(string cmd, CancellationToken ct)
    {
        if (_serialPort == null) return null;
        try { _serialPort.WriteLine(cmd); return _serialPort.ReadLine()?.Trim(); }
        catch (TimeoutException) { return "TIMEOUT"; }
        catch (Exception ex) { throw new Rp2040Exception($"Serial error: {ex.Message}", ex); }
    }
    
    private async Task<string?> AutoDetectPortAsync(CancellationToken ct)
    {
        var ports = new[] { "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10" };
        foreach (var p in ports)
        {
            try
            {
                using var sp = new SerialPort(p, _baudRate) { ReadTimeout = 500, WriteTimeout = 500 };
                sp.Open();
                sp.WriteLine("PING");
                var r = sp.ReadLine();
                if (r?.StartsWith("PONG") == true || r?.StartsWith("OK") == true) return p;
                sp.Close();
            }
            catch { }
            if (ct.IsCancellationRequested) break;
        }
        return null;
    }
    
    public override void Dispose() => DisconnectAsync().Wait();
}
