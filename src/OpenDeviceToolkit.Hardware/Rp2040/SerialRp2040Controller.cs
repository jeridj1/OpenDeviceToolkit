using System.IO.Ports;

namespace OpenDeviceToolkit.Hardware.Rp2040;

/// <summary>
/// RP2040 controller implementation using serial port communication.
/// </summary>
public sealed class SerialRp2040Controller : Rp2040ControllerBase
{
    private SerialPort? _serialPort;
    private string? _portName;
    private int _baudRate = 115200;
    
    public override Rp2040Mode CurrentMode { get; protected set; } = Rp2040Mode.Gpio;
    public override bool IsConnected { get; protected set; }
    public override IReadOnlyList<Rp2040Mode> AvailableModes { get; } = new List<Rp2040Mode>
    {
        Rp2040Mode.Gpio,
        Rp2040Mode.Uart,
        Rp2040Mode.Spi,
        Rp2040Mode.I2c,
        Rp2040Mode.Swd,
        Rp2040Mode.Jtag,
        Rp2040Mode.CmsisDap,
        Rp2040Mode.LogicAnalyzer
    }.AsReadOnly();
    
    /// <summary>
    /// Gets or sets the serial port name.
    /// </summary>
    public string? PortName
    {
        get => _portName;
        set
        {
            _portName = value;
            if (_serialPort != null)
            {
                if (string.IsNullOrEmpty(value))
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                    IsConnected = false;
                }
                else if (_serialPort.PortName != value)
                {
                    _serialPort.Close();
                    _serialPort.PortName = value;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the baud rate.
    /// </summary>
    public int BaudRate
    {
        get => _baudRate;
        set
        {
            _baudRate = value;
            if (_serialPort != null)
                _serialPort.BaudRate = value;
        }
    }
    
    public SerialRp2040Controller(string? portName = null, int baudRate = 115200)
    {
        _portName = portName;
        _baudRate = baudRate;
    }
    
    public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            return true;
        
        if (string.IsNullOrEmpty(_portName))
        {
            // Try to auto-detect RP2040
            _portName = await AutoDetectPortAsync(cancellationToken);
            if (string.IsNullOrEmpty(_portName))
                return false;
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
            
            // Send a ping to verify connection
            await SendCommandAsync("PING", cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _serialPort?.Dispose();
            _serialPort = null;
            IsConnected = false;
            throw new Rp2040Exception("Failed to connect to RP2040", ex);
        }
    }
    
    public override async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_serialPort != null)
        {
            try
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
            }
            catch
            {
                // Ignore errors during disconnect
            }
            
            _serialPort.Dispose();
            _serialPort = null;
        }
        
        IsConnected = false;
    }
    
    public override async Task<bool> SwitchModeAsync(Rp2040Mode mode, CancellationToken cancellationToken = default)
    {
        if (!AvailableModes.Contains(mode))
            return false;
        
        if (!IsConnected)
            await ConnectAsync(cancellationToken);
        
        if (!IsConnected)
            return false;
        
        try
        {
            var command = $"MODE:{mode}";
            var response = await SendCommandAsync(command, cancellationToken);
            
            if (response?.StartsWith("OK") == true)
            {
                CurrentMode = mode;
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            throw new Rp2040Exception($"Failed to switch to mode {mode}", ex);
        }
    }
    
    public override async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            await ConnectAsync(cancellationToken);
        
        if (!IsConnected)
            throw new Rp2040Exception("Not connected to RP2040");
        
        return await SendCommandAsync(command, cancellationToken);
    }
    
    public override async Task<byte[]> ReadAsync(int count, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            await ConnectAsync(cancellationToken);
        
        if (!IsConnected || _serialPort == null)
            throw new Rp2040Exception("Not connected to RP2040");
        
        var buffer = new byte[count];
        var bytesRead = 0;
        
        while (bytesRead < count && !cancellationToken.IsCancellationRequested)
        {
            var read = await _serialPort.BaseStream.ReadAsync(buffer, bytesRead, count - bytesRead, cancellationToken);
            if (read == 0)
                break;
            bytesRead += read;
        }
        
        if (bytesRead < count)
            return buffer.AsSpan(0, bytesRead).ToArray();
        
        return buffer;
    }
    
    public override async Task<int> WriteAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            await ConnectAsync(cancellationToken);
        
        if (!IsConnected || _serialPort == null)
            throw new Rp2040Exception("Not connected to RP2040");
        
        await _serialPort.BaseStream.WriteAsync(data, 0, data.Length, cancellationToken);
        return data.Length;
    }
    
    public override async Task<LogicCapture> CaptureLogicAsync(
        IReadOnlyList<int> pins,
        TimeSpan duration,
        int sampleRateHz,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            await ConnectAsync(cancellationToken);
        
        if (!IsConnected)
            throw new Rp2040Exception("Not connected to RP2040");
        
        // Switch to logic analyzer mode
        await SwitchModeAsync(Rp2040Mode.LogicAnalyzer, cancellationToken);
        
        // Configure pins
        var pinConfigs = pins.Select(p => new Rp2040PinConfig(p, Rp2040PinMode.Input)).ToList();
        await ConfigurePinsAsync(pinConfigs, cancellationToken);
        
        // Send capture command
        var command = $"CAPTURE:{string.Join(",", pins)}:{sampleRateHz}:{duration.TotalMilliseconds}";
        var response = await SendCommandAsync(command, cancellationToken);
        
        if (response?.StartsWith("CAPTURE_OK") != true)
            throw new Rp2040Exception("Failed to start capture");
        
        // Read capture data
        var sampleCount = (int)(duration.TotalSeconds * sampleRateHz);
        var samples = new List<LogicSample>();
        
        for (int i = 0; i < sampleCount; i += 100)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            
            var chunkSize = Math.Min(100, sampleCount - i);
            var data = await ReadAsync(chunkSize * pins.Count, cancellationToken);
            
            // Parse data into samples
            for (int j = 0; j < chunkSize; j++)
            {
                var pinStates = new List<bool>();
                for (int p = 0; p < pins.Count; p++)
                {
                    var byteIndex = j * pins.Count + p;
                    if (byteIndex < data.Length)
                        pinStates.Add(data[byteIndex] != 0);
                    else
                        pinStates.Add(false);
                }
                samples.Add(new LogicSample(
                    Timestamp: TimeSpan.FromSeconds(i / (double)sampleRateHz),
                    PinStates: pinStates.AsReadOnly()
                ));
            }
        }
        
        return new LogicCapture(
            Timestamp: DateTime.UtcNow,
            Duration: duration,
            SampleRateHz: sampleRateHz,
            Pins: pins.ToList().AsReadOnly(),
            Samples: samples.AsReadOnly()
        );
    }
    
    public override async Task<double?> MeasureVoltageAsync(int pin, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            await ConnectAsync(cancellationToken);
        
        if (!IsConnected)
            return null;
        
        var command = $"ADC:{pin}";
        var response = await SendCommandAsync(command, cancellationToken);
        
        if (double.TryParse(response, out var voltage))
            return voltage;
        
        return null;
    }
    
    public override async Task<bool> ConfigurePinsAsync(
        IReadOnlyList<Rp2040PinConfig> pinConfigs,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            await ConnectAsync(cancellationToken);
        
        if (!IsConnected)
            return false;
        
        foreach (var config in pinConfigs)
        {
            var command = $"PIN:{config.PinNumber}:{config.Mode}:{config.PullUp}:{config.PullDown}:{config.InitialState}";
            var response = await SendCommandAsync(command, cancellationToken);
            
            if (response?.StartsWith("OK") != true)
                return false;
        }
        
        return true;
    }
    
    private async Task<string?> SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        if (_serialPort == null)
            return null;
        
        try
        {
            // Write command
            _serialPort.WriteLine(command);
            
            // Read response
            var response = _serialPort.ReadLine();
            return response?.Trim();
        }
        catch (TimeoutException)
        {
            return "TIMEOUT";
        }
        catch (Exception ex)
        {
            throw new Rp2040Exception($"Serial communication error: {ex.Message}", ex);
        }
    }
    
    private async Task<string?> AutoDetectPortAsync(CancellationToken cancellationToken)
    {
        // Try common port names for RP2040
        var commonPorts = new[] { "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10" };
        
        foreach (var portName in commonPorts)
        {
            try
            {
                using var testPort = new SerialPort(portName, _baudRate)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };
                
                testPort.Open();
                testPort.WriteLine("PING");
                var response = testPort.ReadLine();
                
                if (response?.StartsWith("PONG") == true || response?.StartsWith("OK") == true)
                    return portName;
                
                testPort.Close();
            }
            catch
            {
                // Port not available or not RP2040
            }
            
            if (cancellationToken.IsCancellationRequested)
                break;
        }
        
        return null;
    }
    
    public override void Dispose()
    {
        DisconnectAsync().Wait();
    }
}

/// <summary>
/// Exception thrown by RP2040 operations.
/// </summary>
public sealed class Rp2040Exception : Exception
{
    public Rp2040Exception(string message) : base(message) { }
    public Rp2040Exception(string message, Exception inner) : base(message, inner) { }
}
