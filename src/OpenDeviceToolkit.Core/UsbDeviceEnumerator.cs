using System.Management;

namespace OpenDeviceToolkit.Core;

/// <summary>
/// Represents information about a USB device.
/// </summary>
public sealed record UsbDeviceInfo(
    string DeviceId,
    string? Name,
    string? Description,
    string? Manufacturer,
    string? DriverVersion,
    string? DriverProvider,
    string? DriverDate,
    string? UsbClass,
    string? UsbSubclass,
    string? UsbProtocol,
    int? VendorId,
    int? ProductId,
    string? LocationInfo,
    bool IsConnected)
{
    /// <summary>
    /// Gets a user-friendly display name for the device.
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(Name) 
        ? Name 
        : !string.IsNullOrWhiteSpace(Description) 
            ? Description 
            : DeviceId;

    /// <summary>
    /// Gets a string representation of the VID/PID.
    /// </summary>
    public string VidPid => VendorId.HasValue && ProductId.HasValue 
        ? $"VID_{VendorId:X4}&PID_{ProductId:X4}" 
        : string.Empty;
};

/// <summary>
/// Enumerates USB devices and their driver information on Windows.
/// </summary>
public sealed class UsbDeviceEnumerator
{
    private readonly AppLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsbDeviceEnumerator"/> class.
    /// </summary>
    public UsbDeviceEnumerator(AppLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Enumerates all connected USB devices.
    /// </summary>
    public IReadOnlyList<UsbDeviceInfo> EnumerateDevices()
    {
        var devices = new List<UsbDeviceInfo>();

        try
        {
            // Use WMI to query USB devices
            var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_USBControllerDevice");

            foreach (var controllerDevice in searcher.Get())
            {
                try
                {
                    var dependent = (ManagementObject)controllerDevice["Dependent"];
                    var deviceId = dependent["DeviceID"]?.ToString() ?? string.Empty;
                    
                    // Get the associated PnP entity for more details
                    var pnpEntity = GetPnpEntity(deviceId);
                    
                    // Parse VID/PID from DeviceID if available
                    var (vendorId, productId) = ParseVidPid(deviceId);

                    // Get driver information
                    var (driverVersion, driverProvider, driverDate) = GetDriverInfo(deviceId);

                    var deviceInfo = new UsbDeviceInfo(
                        DeviceId: deviceId,
                        Name: pnpEntity?.GetPropertyValue("Name")?.ToString(),
                        Description: pnpEntity?.GetPropertyValue("Description")?.ToString(),
                        Manufacturer: pnpEntity?.GetPropertyValue("Manufacturer")?.ToString(),
                        DriverVersion: driverVersion,
                        DriverProvider: driverProvider,
                        DriverDate: driverDate,
                        UsbClass: pnpEntity?.GetPropertyValue("USBClass")?.ToString(),
                        UsbSubclass: pnpEntity?.GetPropertyValue("USBSubClass")?.ToString(),
                        UsbProtocol: pnpEntity?.GetPropertyValue("USBProtocol")?.ToString(),
                        VendorId: vendorId,
                        ProductId: productId,
                        LocationInfo: pnpEntity?.GetPropertyValue("LocationInfo")?.ToString(),
                        IsConnected: true);

                    devices.Add(deviceInfo);
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Error processing USB controller device: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to enumerate USB devices via WMI", ex);
        }

        // Also try querying PnP entities directly for USB devices
        try
        {
            var pnpSearcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'USB' OR PNPClass = 'AndroidAdbInterface'");

            foreach (var pnpEntity in pnpSearcher.Get())
            {
                try
                {
                    var deviceId = pnpEntity["DeviceID"]?.ToString() ?? string.Empty;
                    var name = pnpEntity["Name"]?.ToString();
                    
                    // Check if we already have this device
                    if (devices.Any(d => d.DeviceId == deviceId))
                    {
                        continue;
                    }

                    var (vendorId, productId) = ParseVidPid(deviceId);
                    var (driverVersion, driverProvider, driverDate) = GetDriverInfo(deviceId);

                    var deviceInfo = new UsbDeviceInfo(
                        DeviceId: deviceId,
                        Name: name,
                        Description: pnpEntity["Description"]?.ToString(),
                        Manufacturer: pnpEntity["Manufacturer"]?.ToString(),
                        DriverVersion: driverVersion,
                        DriverProvider: driverProvider,
                        DriverDate: driverDate,
                        UsbClass: pnpEntity["USBClass"]?.ToString(),
                        UsbSubclass: pnpEntity["USBSubClass"]?.ToString(),
                        UsbProtocol: pnpEntity["USBProtocol"]?.ToString(),
                        VendorId: vendorId,
                        ProductId: productId,
                        LocationInfo: pnpEntity["LocationInfo"]?.ToString(),
                        IsConnected: true);

                    devices.Add(deviceInfo);
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Error processing PnP entity: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to enumerate PnP entities via WMI", ex);
        }

        return devices;
    }

    /// <summary>
    /// Gets the PnP entity for a given device ID.
    /// </summary>
    private ManagementObject? GetPnpEntity(string deviceId)
    {
        try
        {
            var query = $"SELECT * FROM Win32_PnPEntity WHERE DeviceID = '{deviceId.Replace("\\", "\\\\")}'";
            var searcher = new ManagementObjectSearcher(query);
            var results = searcher.Get();
            return results.Cast<ManagementObject>().FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses Vendor ID and Product ID from a device ID string.
    /// </summary>
    private (int? VendorId, int? ProductId) ParseVidPid(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return (null, null);
        }

        // DeviceID format: USB\VID_XXXX&PID_XXXX\XXXX
        var vidIndex = deviceId.IndexOf("VID_", StringComparison.OrdinalIgnoreCase);
        var pidIndex = deviceId.IndexOf("PID_", StringComparison.OrdinalIgnoreCase);

        if (vidIndex < 0 || pidIndex < 0)
        {
            return (null, null);
        }

        try
        {
            var vidStr = deviceId.Substring(vidIndex + 4, 4);
            var pidStr = deviceId.Substring(pidIndex + 4, 4);

            if (int.TryParse(vidStr, System.Globalization.NumberStyles.HexNumber, null, out var vendorId) &&
                int.TryParse(pidStr, System.Globalization.NumberStyles.HexNumber, null, out var productId))
            {
                return (vendorId, productId);
            }
        }
        catch
        {
            // Parsing failed
        }

        return (null, null);
    }

    /// <summary>
    /// Gets driver information for a device.
    /// </summary>
    private (string? Version, string? Provider, string? Date) GetDriverInfo(string deviceId)
    {
        try
        {
            var query = $"ASSOCIATORS OF {{Win32_PnPEntity.DeviceID='{deviceId.Replace("\\", "\\\\")}'}} WHERE AssocClass = Win32_PnPEntityDriver";
            var searcher = new ManagementObjectSearcher(query);
            var results = searcher.Get();

            foreach (var driver in results.Cast<ManagementObject>())
            {
                var version = driver["DriverVersion"]?.ToString();
                var provider = driver["ProviderName"]?.ToString();
                var date = driver["DriverDate"]?.ToString();

                if (!string.IsNullOrWhiteSpace(version) || 
                    !string.IsNullOrWhiteSpace(provider) || 
                    !string.IsNullOrWhiteSpace(date))
                {
                    return (version, provider, date);
                }
            }
        }
        catch
        {
            // Driver info not available
        }

        return (null, null, null);
    }

    /// <summary>
    /// Filters devices to only Android devices (based on VID/PID or interface class).
    /// </summary>
    public IReadOnlyList<UsbDeviceInfo> GetAndroidDevices(IEnumerable<UsbDeviceInfo>? devices = null)
    {
        var allDevices = devices ?? EnumerateDevices();
        var androidDevices = new List<UsbDeviceInfo>();

        // Known Android USB VIDs (Google, LG, Samsung, etc.)
        var androidVendors = new HashSet<int>
        {
            0x18D1, // Google
            0x1004, // LG
            0x04E8, // Samsung
            0x2207, // HTC
            0x0BB4, // HTC
            0x2717, // OnePlus
            0x2976, // Xiaomi
            0x05C6, // Qualcomm
            0x0FCE, // Sony
            0x19D2, // ZTE
            0x201E, // Haier
            0x22B8, // Motorola
            0x2A47, // Oppo
            0x2C3F, // Realme
            0x2E1A, // Vivo
        };

        foreach (var device in allDevices)
        {
            // Check if it's an Android device by VID
            if (device.VendorId.HasValue && androidVendors.Contains(device.VendorId.Value))
            {
                androidDevices.Add(device);
                continue;
            }

            // Check by interface class (Android ADB interface)
            if (device.UsbClass == "FF" && device.UsbSubclass == "42" && device.UsbProtocol == "01")
            {
                androidDevices.Add(device);
                continue;
            }

            // Check by device ID pattern
            if (device.DeviceId.Contains("AndroidAdbInterface") ||
                device.DeviceId.Contains("ADB") ||
                device.Name?.Contains("ADB") == true ||
                device.Description?.Contains("ADB") == true)
            {
                androidDevices.Add(device);
            }
        }

        return androidDevices;
    }

    /// <summary>
    /// Checks if a specific Android device is connected via USB.
    /// </summary>
    public bool IsAndroidDeviceConnected(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return false;
        }

        var devices = EnumerateDevices();
        foreach (var device in devices)
        {
            // Check if serial is in device ID or name
            if (device.DeviceId.Contains(serialNumber, StringComparison.OrdinalIgnoreCase) ||
                (device.Name?.Contains(serialNumber, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a summary of USB device inventory for diagnostics.
    /// </summary>
    public DiagnosticItem GetUsbInventorySummary()
    {
        try
        {
            var devices = EnumerateDevices();
            var androidDevices = GetAndroidDevices(devices);
            var otherDevices = devices.Count - androidDevices.Count;

            var summary = $"{devices.Count} USB devices, {androidDevices.Count} Android, {otherDevices} other";
            return new DiagnosticItem("USB Device Inventory", DiagnosticStatus.Pass, summary);
        }
        catch (Exception ex)
        {
            return new DiagnosticItem("USB Device Inventory", DiagnosticStatus.Fail, "Error enumerating devices", ex.Message);
        }
    }
}
