using System.IO.Ports;

namespace OpenDeviceToolkit.Hardware;

public sealed class Rp2040BridgeClient : IDisposable
{
    private SerialPort? _port;
    public bool IsConnected => _port?.IsOpen == true;

    public void Connect(string portName, int baudRate = 115200)
    {
        Disconnect();
        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One) { ReadTimeout = 1000, WriteTimeout = 1000, NewLine = "\n" };
        _port.Open();
    }

    public string Command(string command)
    {
        if (_port is not { IsOpen: true }) throw new InvalidOperationException("RP2040 bridge is not connected.");
        _port.WriteLine(command);
        return _port.ReadLine().Trim();
    }

    public string Hello() => Command("HELLO");
    public string Safe() => Command("SAFE");
    public string MeasureTargetVoltage() => Command("VOLTAGE");
    public string ConfigureSniffer(IEnumerable<int> gpios) => Command($"SNIFF {string.Join(',', gpios)}");
    public string Sample() => Command("SAMPLE");

    public void Disconnect()
    {
        if (_port is null) return;
        try { if (_port.IsOpen) _port.Close(); } finally { _port.Dispose(); _port = null; }
    }

    public void Dispose() => Disconnect();
}
