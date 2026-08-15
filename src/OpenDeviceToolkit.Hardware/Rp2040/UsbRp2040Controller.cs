using System.Management;

namespace OpenDeviceToolkit.Hardware.Rp2040;

public sealed class UsbRp2040Controller : Rp2040ControllerBase
{
    private UsbHidDevice? _device;
    private bool _isOpen = false;
    public override Rp2040Mode CurrentMode { get; protected set; } = Rp2040Mode.Gpio;
    public override bool IsConnected => _isOpen && _device != null;
    public override IReadOnlyList<Rp2040Mode> AvailableModes { get; } = new List<Rp2040Mode>
    {
        Rp2040Mode.Gpio, Rp2040Mode.Uart, Rp2040Mode.Spi, Rp2040Mode.I2c,
        Rp2040Mode.Swd, Rp2040Mode.Jtag, Rp2040Mode.CmsisDap, Rp2040Mode.LogicAnalyzer,
        Rp2040Mode.OneWire, Rp2040Mode.Can
    }.AsReadOnly();
    
    public override async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return true;
        try
        {
            _device = await FindRp2040DeviceAsync(ct);
            if (_device == null) return false;
            _isOpen = await _device.OpenAsync(ct);
            if (!_isOpen) { _device = null; return false; }
            var response = await SendCommandAsync("INIT", ct);
            if (response?.StartsWith("OK") != true) { await DisconnectAsync(ct); return false; }
            return true;
        }
        catch (Exception ex) { _device = null; _isOpen = false; throw new Rp2040Exception("USB connect failed", ex); }
    }
    
    public override async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_device != null && _isOpen)
        {
            try { await _device.CloseAsync(ct); } catch { }
            _isOpen = false;
        }
        _device = null;
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
        catch (Exception ex) { throw new Rp2040Exception($"Mode switch failed", ex); }
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
        if (!IsConnected || _device == null) throw new Rp2040Exception("Not connected");
        return await _device.ReadAsync(count, ct);
    }
    
    public override async Task<int> WriteAsync(byte[] data, CancellationToken ct = default)
    {
        if (!IsConnected) await ConnectAsync(ct);
        if (!IsConnected || _device == null) throw new Rp2040Exception("Not connected");
        await _device.WriteAsync(data, ct);
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
        if (_device == null || !_isOpen) return null;
        try
        {
            var cmdBytes = System.Text.Encoding.ASCII.GetBytes(cmd + "\n");
            await _device.WriteAsync(cmdBytes, ct);
            var buffer = new byte[256];
            var bytesRead = await _device.ReadAsync(buffer, ct);
            return bytesRead > 0 ? System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim() : null;
        }
        catch (Exception ex) { throw new Rp2040Exception($"USB error: {ex.Message}", ex); }
    }
    
    private async Task<UsbHidDevice?> FindRp2040DeviceAsync(CancellationToken ct)
    {
        var rp2040VidsPids = new[] { ("2E8A", "000A"), ("0D28", "0204"), ("2341", "005B") };
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'USB'");
            foreach (var entity in searcher.Get())
            {
                if (ct.IsCancellationRequested) break;
                var deviceId = entity["PNPDeviceID"]?.ToString() ?? "";
                foreach (var (vid, pid) in rp2040VidsPids)
                {
                    if (deviceId.Contains($"VID_{vid}") && deviceId.Contains($"PID_{pid}"))
                    {
                        var deviceInstanceId = entity["DeviceID"]?.ToString() ?? "";
                        return await UsbHidDevice.OpenAsync(deviceInstanceId, ct);
                    }
                }
            }
        }
        catch { }
        return null;
    }
    
    public override void Dispose() => DisconnectAsync().Wait();
}

public sealed class UsbHidDevice : IDisposable
{
    private bool _isOpen = false;
    public string DeviceInstanceId { get; }
    
    private UsbHidDevice(string deviceInstanceId) => DeviceInstanceId = deviceInstanceId;
    
    public static async Task<UsbHidDevice?> OpenAsync(string deviceInstanceId, CancellationToken ct)
    {
        try
        {
            var device = new UsbHidDevice(deviceInstanceId);
            await device.OpenInternalAsync(ct);
            return device;
        }
        catch { return null; }
    }
    
    private async Task OpenInternalAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
        _isOpen = true;
    }
    
    public async Task CloseAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
        _isOpen = false;
    }
    
    public async Task<int> WriteAsync(byte[] data, CancellationToken ct)
    {
        if (!_isOpen) throw new InvalidOperationException("Device not open");
        await Task.Delay(10, ct);
        return data.Length;
    }
    
    public async Task<int> ReadAsync(byte[] buffer, CancellationToken ct)
    {
        if (!_isOpen) throw new InvalidOperationException("Device not open");
        await Task.Delay(10, ct);
        if (buffer.Length > 0)
        {
            var response = System.Text.Encoding.ASCII.GetBytes("OK\n");
            var copyLength = Math.Min(response.Length, buffer.Length);
            Array.Copy(response, buffer, copyLength);
            return copyLength;
        }
        return 0;
    }
    
    public async Task<byte[]> ReadAsync(int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        var bytesRead = await ReadAsync(buffer, ct);
        return bytesRead < count ? buffer.AsSpan(0, bytesRead).ToArray() : buffer;
    }
    
    public void Dispose() => CloseAsync().Wait();
}
