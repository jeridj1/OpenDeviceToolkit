using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDeviceToolkit.Hardware.Rp2040;

/// <summary>
/// Database of known chip pinouts for automatic RP2040 configuration.
/// Used for auto-wiring guidance when connecting to target devices.
/// </summary>
public static class PinoutDatabase
{
    private static Dictionary<string, ChipPinout>? _database;
    
    /// <summary>
    /// Gets a chip pinout by its identifier (name, VID/PID, manufacturer, etc.).
    /// </summary>
    public static ChipPinout? GetPinout(string identifier)
    {
        LoadDatabase();
        if (_database == null) return null;
        
        if (_database.TryGetValue(identifier, out var pinout)) return pinout;
        
        foreach (var kvp in _database)
        {
            if (kvp.Key.Equals(identifier, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
            if (kvp.Key.Contains(identifier, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets all known chip identifiers.
    /// </summary>
    public static IReadOnlyList<string> GetChipIdentifiers()
    {
        LoadDatabase();
        return _database?.Keys.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }
    
    /// <summary>
    /// Adds or updates a chip pinout in the database.
    /// </summary>
    public static void AddPinout(string identifier, ChipPinout pinout)
    {
        LoadDatabase();
        _database ??= new Dictionary<string, ChipPinout>();
        _database[identifier] = pinout;
        SaveDatabase();
    }
    
    /// <summary>
    /// Finds pinouts that match the given USB VID/PID.
    /// </summary>
    public static IReadOnlyList<ChipPinout> FindByUsbId(string vid, string pid)
    {
        LoadDatabase();
        var results = new List<ChipPinout>();
        
        if (_database == null) return results;
        
        foreach (var kvp in _database)
        {
            if (kvp.Value.UsbVids?.Contains(vid, StringComparer.OrdinalIgnoreCase) == true &&
                kvp.Value.UsbPids?.Contains(pid, StringComparer.OrdinalIgnoreCase) == true)
            {
                results.Add(kvp.Value);
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Gets pin configuration for a specific protocol on a chip.
    /// </summary>
    public static IReadOnlyList<Rp2040PinConfig>? GetProtocolPins(string chipIdentifier, string protocolType)
    {
        var pinout = GetPinout(chipIdentifier);
        if (pinout == null) return null;
        
        var protocol = pinout.ProgrammingInterfaces.FirstOrDefault(p =>
            p.Type.Equals(protocolType, StringComparison.OrdinalIgnoreCase));
        
        if (protocol == null) return null;
        
        var configs = new List<Rp2040PinConfig>();
        
        if (protocol.ClockPin.HasValue)
            configs.Add(new Rp2040PinConfig(protocol.ClockPin.Value, Rp2040PinMode.Output));
        
        if (protocol.DataPin.HasValue)
            configs.Add(new Rp2040PinConfig(protocol.DataPin.Value, Rp2040PinMode.Input));
        
        if (protocol.ResetPin.HasValue)
            configs.Add(new Rp2040PinConfig(protocol.ResetPin.Value, Rp2040PinMode.Output));
        
        return configs;
    }
    
    /// <summary>
    /// Gets wiring instructions for connecting RP2040 to a target chip.
    /// </summary>
    public static string GetWiringInstructions(string chipIdentifier, string protocolType)
    {
        var pinout = GetPinout(chipIdentifier);
        if (pinout == null) return $"Unknown chip: {chipIdentifier}";
        
        var protocol = pinout.ProgrammingInterfaces.FirstOrDefault(p =>
            p.Type.Equals(protocolType, StringComparison.OrdinalIgnoreCase));
        
        if (protocol == null) return $"Unknown protocol: {protocolType} for {chipIdentifier}";
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Connecting RP2040 to {pinout.Manufacturer} {pinout.Name} for {protocolType}:");
        sb.AppendLine($"Target Voltage: {protocol.Voltage}V");
        sb.AppendLine();
        
        if (protocol.ClockPin.HasValue)
            sb.AppendLine($"  RP2040 GP2 (SWCLK)  -> {pinout.Name} Pin {protocol.ClockPin} ({GetPinName(pinout, protocol.ClockPin.Value)})");
        
        if (protocol.DataPin.HasValue)
            sb.AppendLine($"  RP2040 GP3 (SWDIO) -> {pinout.Name} Pin {protocol.DataPin} ({GetPinName(pinout, protocol.DataPin.Value)})");
        
        if (protocol.ResetPin.HasValue)
            sb.AppendLine($"  RP2040 GP4        -> {pinout.Name} Pin {protocol.ResetPin} ({GetPinName(pinout, protocol.ResetPin.Value)}) [Optional]");
        
        sb.AppendLine();
        sb.AppendLine("  RP2040 GND       -> Target GND");
        sb.AppendLine($"  RP2040 3V3      -> Target {protocol.Voltage}V");
        
        if (!string.IsNullOrEmpty(protocol.Notes))
        {
            sb.AppendLine();
            sb.AppendLine($"Notes: {protocol.Notes}");
        }
        
        return sb.ToString();
    }
    
    private static string GetPinName(ChipPinout pinout, int pinNumber)
    {
        if (pinout.Pins.TryGetValue(pinNumber, out var pinInfo)) return pinInfo.Name;
        return $"Pin {pinNumber}";
    }
    
    private static void LoadDatabase()
    {
        if (_database != null) return;
        
        _database = new Dictionary<string, ChipPinout>();
        
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "pinouts.json");
            if (File.Exists(path))
                _database = JsonSerializer.Deserialize<Dictionary<string, ChipPinout>>(File.ReadAllText(path)) ?? _database;
        }
        catch { }
        
        AddBuiltInPinouts();
    }
    
    private static void AddBuiltInPinouts()
    {
        // STM32F103 (Blue Pill)
        if (!_database.ContainsKey("STM32F103"))
            _database["STM32F103"] = new ChipPinout
            {
                Name = "STM32F103",
                Manufacturer = "STMicroelectronics",
                Description = "STM32F103 ARM Cortex-M3 (Blue Pill)",
                UsbVids = new List<string> { "0483" },
                UsbPids = new List<string> { "5740", "DF11" },
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface
                    {
                        Type = "SWD",
                        ClockPin = 3,
                        DataPin = 2,
                        Voltage = 3.3,
                        Notes = "PA14=SWCLK, PA13=SWDIO. Standard ARM SWD interface."
                    },
                    new ProgrammingInterface
                    {
                        Type = "UART",
                        DataPin = 20,
                        Voltage = 3.3,
                        Notes = "PA9=TX, PA10=RX. Baud rate: 115200."
                    }
                }
            };
        
        // RP2040 (Raspberry Pi Pico)
        if (!_database.ContainsKey("RP2040"))
            _database["RP2040"] = new ChipPinout
            {
                Name = "RP2040",
                Manufacturer = "Raspberry Pi",
                Description = "RP2040 Dual-Core ARM Cortex-M0+",
                UsbVids = new List<string> { "2E8A" },
                UsbPids = new List<string> { "000A", "0005" },
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface
                    {
                        Type = "UART",
                        DataPin = 0,
                        Voltage = 3.3,
                        Notes = "GP0=TX, GP1=RX. Baud rate: 115200."
                    },
                    new ProgrammingInterface
                    {
                        Type = "SWD",
                        ClockPin = 17,
                        DataPin = 16,
                        ResetPin = 24,
                        Voltage = 3.3,
                        Notes = "GP17=SWCLK, GP16=SWDIO, RUN=Reset (active low)."
                    }
                }
            };
        
        // ESP32
        if (!_database.ContainsKey("ESP32"))
            _database["ESP32"] = new ChipPinout
            {
                Name = "ESP32",
                Manufacturer = "Espressif",
                Description = "ESP32 Dual-Core Xtensa LX6",
                UsbVids = new List<string> { "10C4", "303A" },
                UsbPids = new List<string> { "EA60", "1001" },
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface
                    {
                        Type = "UART",
                        DataPin = 0,
                        Voltage = 3.3,
                        Notes = "TX0/RX0. Baud rate: 115200. Hold BOOT button while powering on for bootloader mode."
                    }
                }
            };
        
        // ATmega328P (Arduino Uno/Nano)
        if (!_database.ContainsKey("ATmega328P"))
            _database["ATmega328P"] = new ChipPinout
            {
                Name = "ATmega328P",
                Manufacturer = "Microchip",
                Description = "ATmega328P 8-bit AVR Microcontroller",
                UsbVids = new List<string> { "2341", "16C0", "1A86" },
                UsbPids = new List<string> { "0043", "0483", "7523" },
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface
                    {
                        Type = "AVR ISP",
                        ClockPin = 13,
                        DataPin = 12,
                        ResetPin = 10,
                        Voltage = 5.0,
                        Notes = "SCK=13, MISO=12, MOSI=11, RESET=10. Use Arduino as ISP or USBasp programmer."
                    },
                    new ProgrammingInterface
                    {
                        Type = "UART",
                        DataPin = 1,
                        Voltage = 5.0,
                        Notes = "TX=1, RX=0. Baud rate: 57600 for Arduino Uno bootloader."
                    }
                }
            };
        
        // Qualcomm Snapdragon 855 (msmnile) - LG V50
        if (!_database.ContainsKey("MSMNILE"))
            _database["MSMNILE"] = new ChipPinout
            {
                Name = "Snapdragon 855",
                Manufacturer = "Qualcomm",
                Description = "Snapdragon 855 Mobile Platform (msmnile)",
                UsbVids = new List<string> { "05C6" },
                UsbPids = new List<string> { "9008", "900E", "9040" },
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface
                    {
                        Type = "EDL",
                        Voltage = 1.8,
                        Notes = "Emergency Download Mode (9008). Short test points near USB connector or use button combination."
                    }
                }
            };
    }
    
    private static void SaveDatabase()
    {
        if (_database == null) return;
        
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "pinouts.json");
            File.WriteAllText(path, JsonSerializer.Serialize(_database, 
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}

public sealed class ChipPinout
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("usbVids")] public IReadOnlyList<string>? UsbVids { get; set; }
    [JsonPropertyName("usbPids")] public IReadOnlyList<string>? UsbPids { get; set; }
    [JsonPropertyName("pins")] public Dictionary<int, PinInfo> Pins { get; set; } = new();
    [JsonPropertyName("programmingInterfaces")] public IReadOnlyList<ProgrammingInterface> ProgrammingInterfaces { get; set; } = new List<ProgrammingInterface>();
}

public sealed class PinInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("type")] public PinType Type { get; set; } = PinType.Gpio;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("alternateFunctions")] public IReadOnlyList<string>? AlternateFunctions { get; set; }
}

public enum PinType { Power, Ground, Gpio, Analog, Clock, Reset, Special }

public sealed class ProgrammingInterface
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("clockPin")] public int? ClockPin { get; set; }
    [JsonPropertyName("dataPin")] public int? DataPin { get; set; }
    [JsonPropertyName("resetPin")] public int? ResetPin { get; set; }
    [JsonPropertyName("voltage")] public double Voltage { get; set; } = 3.3;
    [JsonPropertyName("notes")] public string? Notes { get; set; }
}
