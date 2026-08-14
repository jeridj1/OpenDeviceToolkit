using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDeviceToolkit.Hardware.Rp2040;

/// <summary>
/// Database of known chip pinouts for automatic configuration.
/// </summary>
public static class PinoutDatabase
{
    private static Dictionary<string, ChipPinout>? _database;
    
    /// <summary>
    /// Gets a chip pinout by its identifier (name, VID/PID, etc.).
    /// </summary>
    public static ChipPinout? GetPinout(string identifier)
    {
        LoadDatabase();
        
        if (_database == null)
            return null;
        
        // Try exact match
        if (_database.TryGetValue(identifier, out var pinout))
            return pinout;
        
        // Try case-insensitive match
        foreach (var kvp in _database)
        {
            if (kvp.Key.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }
        
        // Try partial match
        foreach (var kvp in _database)
        {
            if (kvp.Key.Contains(identifier, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
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
        
        if (_database == null)
            return results;
        
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
    
    private static void LoadDatabase()
    {
        if (_database != null)
            return;
        
        _database = new Dictionary<string, ChipPinout>();
        
        // Load from embedded resource or file
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "pinouts.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _database = JsonSerializer.Deserialize<Dictionary<string, ChipPinout>>(json) ?? _database;
            }
        }
        catch
        {
            // Use default database
        }
        
        // Add built-in pinouts
        AddBuiltInPinouts();
    }
    
    private static void AddBuiltInPinouts()
    {
        // Add common chip pinouts here
        // Example: STM32F103 (Blue Pill)
        if (!_database.ContainsKey("STM32F103"))
        {
            _database["STM32F103"] = new ChipPinout
            {
                Name = "STM32F103",
                Manufacturer = "STMicroelectronics",
                Description = "STM32F103 ARM Cortex-M3 Microcontroller",
                UsbVids = new List<string> { "0483" }, // STMicroelectronics
                UsbPids = new List<string> { "5740" },
                Pins = new Dictionary<int, PinInfo>
                {
                    {1, new PinInfo { Name = "3V3", Type = PinType.Power, Description = "3.3V Power" }},
                    {2, new PinInfo { Name = "PA13", Type = PinType.Gpio, Description = "SWDIO", AlternateFunctions = new[] { "SWDIO" } }},
                    {3, new PinInfo { Name = "PA14", Type = PinType.Gpio, Description = "SWCLK", AlternateFunctions = new[] { "SWCLK" } }},
                    {4, new PinInfo { Name = "GND", Type = PinType.Ground, Description = "Ground" }},
                    {5, new PinInfo { Name = "PA15", Type = PinType.Gpio, Description = "TIM2_CH1_ETR", AlternateFunctions = new[] { "TIM2_CH1", "SPI1_NSS" } }},
                    {6, new PinInfo { Name = "PB3", Type = PinType.Gpio, Description = "SPI1_SCK", AlternateFunctions = new[] { "SPI1_SCK", "TIM2_CH2" } }},
                    {7, new PinInfo { Name = "PB4", Type = PinType.Gpio, Description = "SPI1_MISO", AlternateFunctions = new[] { "SPI1_MISO" } }},
                    {8, new PinInfo { Name = "PB5", Type = PinType.Gpio, Description = "SPI1_MOSI", AlternateFunctions = new[] { "SPI1_MOSI" } }},
                    {9, new PinInfo { Name = "PB6", Type = PinType.Gpio, Description = "I2C1_SCL", AlternateFunctions = new[] { "I2C1_SCL", "TIM4_CH1" } }},
                    {10, new PinInfo { Name = "PB7", Type = PinType.Gpio, Description = "I2C1_SDA", AlternateFunctions = new[] { "I2C1_SDA", "TIM4_CH2" } }}
                },
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface
                    {
                        Type = "SWD",
                        ClockPin = 3,  // PA14/SWCLK
                        DataPin = 2,   // PA13/SWDIO
                        ResetPin = null,
                        Voltage = 3.3
                    },
                    new ProgrammingInterface
                    {
                        Type = "ST-Link",
                        ClockPin = 3,
                        DataPin = 2,
                        ResetPin = null,
                        Voltage = 3.3
                    }
                }
            };
        }
        
        // Add RP2040 itself
        if (!_database.ContainsKey("RP2040"))
        {
            _database["RP2040"] = new ChipPinout
            {
                Name = "RP2040",
                Manufacturer = "Raspberry Pi",
                Description = "RP2040 Dual-Core ARM Cortex-M0+ Microcontroller",
                UsbVids = new List<string> { "2E8A" },
                UsbPids = new List<string> { "000A" },
                Pins = new Dictionary<int, PinInfo>(),
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface
                    {
                        Type = "UART",
                        ClockPin = null,
                        DataPin = 0,  // GP0/UART0_TX
                        ResetPin = null,
                        Voltage = 3.3
                    },
                    new ProgrammingInterface
                    {
                        Type = "SWD",
                        ClockPin = 2,  // GP2
                        DataPin = 3,  // GP3
                        ResetPin = null,
                        Voltage = 3.3
                    }
                }
            };
        }
    }
    
    private static void SaveDatabase()
    {
        if (_database == null)
            return;
        
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "pinouts.json");
            var json = JsonSerializer.Serialize(_database, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}

/// <summary>
/// Information about a microcontroller or chip.
/// </summary>
public sealed class ChipPinout
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("usbVids")]
    public IReadOnlyList<string>? UsbVids { get; set; }
    
    [JsonPropertyName("usbPids")]
    public IReadOnlyList<string>? UsbPids { get; set; }
    
    [JsonPropertyName("pins")]
    public Dictionary<int, PinInfo> Pins { get; set; } = new();
    
    [JsonPropertyName("programmingInterfaces")]
    public IReadOnlyList<ProgrammingInterface> ProgrammingInterfaces { get; set; } = new List<ProgrammingInterface>();
}

/// <summary>
/// Information about a single pin.
/// </summary>
public sealed class PinInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public PinType Type { get; set; } = PinType.Gpio;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("alternateFunctions")]
    public IReadOnlyList<string>? AlternateFunctions { get; set; }
}

/// <summary>
/// Type of pin.
/// </summary>
public enum PinType
{
    Power,
    Ground,
    Gpio,
    Analog,
    Clock,
    Reset,
    Special
}

/// <summary>
/// Information about a programming interface for a chip.
/// </summary>
public sealed class ProgrammingInterface
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("clockPin")]
    public int? ClockPin { get; set; }
    
    [JsonPropertyName("dataPin")]
    public int? DataPin { get; set; }
    
    [JsonPropertyName("resetPin")]
    public int? ResetPin { get; set; }
    
    [JsonPropertyName("voltage")]
    public double Voltage { get; set; } = 3.3;
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
