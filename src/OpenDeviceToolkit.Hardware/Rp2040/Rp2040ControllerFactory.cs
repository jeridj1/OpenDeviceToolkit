namespace OpenDeviceToolkit.Hardware.Rp2040;

/// <summary>
/// Factory for creating RP2040 controllers based on transport type.
/// </summary>
public static class Rp2040ControllerFactory
{
    private static IRp2040Controller? _defaultController;
    private static TransportType _defaultTransport = TransportType.Serial;
    
    /// <summary>
    /// Gets or creates the default RP2040 controller.
    /// </summary>
    public static IRp2040Controller GetController()
    {
        if (_defaultController == null)
        {
            _defaultController = Create(_defaultTransport);
        }
        return _defaultController;
    }
    
    /// <summary>
    /// Gets or creates a controller for a specific transport.
    /// </summary>
    public static IRp2040Controller GetController(TransportType transport)
    {
        _defaultTransport = transport;
        _defaultController = Create(transport);
        return _defaultController;
    }
    
    /// <summary>
    /// Creates a controller for a specific transport.
    /// </summary>
    public static IRp2040Controller Create(TransportType transport)
    {
        return transport switch
        {
            TransportType.Serial => new SerialRp2040Controller(),
            TransportType.Usb => new UsbRp2040Controller(),
            TransportType.Bluetooth => new MockRp2040Controller(), // Bluetooth not yet implemented
            TransportType.Network => new MockRp2040Controller(), // Network not yet implemented
            _ => new MockRp2040Controller()
        };
    }
    
    /// <summary>
    /// Creates a controller with specific settings.
    /// </summary>
    public static IRp2040Controller Create(TransportType transport, string portName, int baudRate = 115200)
    {
        return transport switch
        {
            TransportType.Serial => new SerialRp2040Controller(portName, baudRate),
            TransportType.Usb => new UsbRp2040Controller(),
            _ => new SerialRp2040Controller(portName, baudRate)
        };
    }
    
    /// <summary>
    /// Sets the default transport type.
    /// </summary>
    public static void SetDefaultTransport(TransportType transport)
    {
        _defaultTransport = transport;
        _defaultController = null; // Force recreation on next GetController()
    }
    
    /// <summary>
    /// Lists all available COM ports.
    /// </summary>
    public static IReadOnlyList<string> ListSerialPorts()
    {
        try
        {
            return System.IO.Ports.SerialPort.GetPortNames().ToList().AsReadOnly();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}

/// <summary>
/// Transport types for RP2040 communication.
/// </summary>
public enum TransportType
{
    Serial,
    Usb,
    Bluetooth,
    Network
}
