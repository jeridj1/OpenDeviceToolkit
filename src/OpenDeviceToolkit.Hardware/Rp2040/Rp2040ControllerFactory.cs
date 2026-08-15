namespace OpenDeviceToolkit.Hardware.Rp2040;

public static class Rp2040ControllerFactory
{
    private static IRp2040Controller? _defaultController;
    private static TransportType _defaultTransport = TransportType.Serial;
    
    public static IRp2040Controller GetController()
    {
        if (_defaultController == null) _defaultController = Create(_defaultTransport);
        return _defaultController;
    }
    
    public static IRp2040Controller GetController(TransportType transport)
    {
        _defaultTransport = transport;
        _defaultController = Create(transport);
        return _defaultController;
    }
    
    public static IRp2040Controller Create(TransportType transport)
    {
        return transport switch
        {
            TransportType.Serial => new SerialRp2040Controller(),
            TransportType.Usb => new UsbRp2040Controller(),
            _ => new MockRp2040Controller()
        };
    }
    
    public static IRp2040Controller Create(TransportType transport, string portName, int baudRate = 115200)
    {
        return transport switch
        {
            TransportType.Serial => new SerialRp2040Controller(portName, baudRate),
            _ => new SerialRp2040Controller(portName, baudRate)
        };
    }
    
    public static void SetDefaultTransport(TransportType transport)
    {
        _defaultTransport = transport;
        _defaultController = null;
    }
    
    public static IReadOnlyList<string> ListSerialPorts()
    {
        try { return System.IO.Ports.SerialPort.GetPortNames().ToList().AsReadOnly(); }
        catch { return Array.Empty<string>(); }
    }
}

public enum TransportType { Serial, Usb, Bluetooth, Network }
