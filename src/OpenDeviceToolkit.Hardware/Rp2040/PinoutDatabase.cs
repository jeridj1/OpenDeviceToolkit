using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDeviceToolkit.Hardware.Rp2040;

public static class PinoutDatabase
{
    private static Dictionary<string, ChipPinout>? _database;
    
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
    
    public static IReadOnlyList<string> GetChipIdentifiers()
    {
        LoadDatabase();
        return _database?.Keys.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }
    
    public static void AddPinout(string identifier, ChipPinout pinout)
    {
        LoadDatabase();
        _database ??= new Dictionary<string, ChipPinout>();
        _database[identifier] = pinout;
        SaveDatabase();
    }
    
    public static IReadOnlyList<ChipPinout> FindByUsbId(string vid, string pid)
    {
        LoadDatabase();
        var results = new List<ChipPinout>();
        if (_database == null) return results;
        foreach (var kvp in _database)
        {
            if (kvp.Value.UsbVids?.Contains(vid, StringComparer.OrdinalIgnoreCase) == true &&
                kvp.Value.UsbPids?.Contains(pid, StringComparer.OrdinalIgnoreCase) == true)
                results.Add(kvp.Value);
        }
        return results;
    }
    
    public static IReadOnlyList<Rp2040PinConfig>? GetProtocolPins(string chipIdentifier, string protocolType)
    {
        var pinout = GetPinout(chipIdentifier);
        if (pinout == null) return null;
        var protocol = pinout.ProgrammingInterfaces.FirstOrDefault(p =>
            p.Type.Equals(protocolType, StringComparison.OrdinalIgnoreCase));
        if (protocol == null) return null;
        var configs = new List<Rp2040PinConfig>();
        if (protocol.ClockPin.HasValue) configs.Add(new Rp2040PinConfig(protocol.ClockPin.Value, Rp2040PinMode.Output));
        if (protocol.DataPin.HasValue) configs.Add(new Rp2040PinConfig(protocol.DataPin.Value, Rp2040PinMode.Input));
        if (protocol.ResetPin.HasValue) configs.Add(new Rp2040PinConfig(protocol.ResetPin.Value, Rp2040PinMode.Output));
        return configs;
    }
    
    private static void LoadDatabase()
    {
        if (_database != null) return;
        _database = new Dictionary<string, ChipPinout>();
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "pinouts.json");
            if (File.Exists(path)) _database = JsonSerializer.Deserialize<Dictionary<string, ChipPinout>>(File.ReadAllText(path)) ?? _database;
        }
        catch { }
        AddBuiltInPinouts();
    }
    
    private static void AddBuiltInPinouts()
    {
        if (!_database.ContainsKey("STM32F103"))
            _database["STM32F103"] = new ChipPinout
            {
                Name = "STM32F103",
                Manufacturer = "STMicroelectronics",
                Description = "STM32F103 ARM Cortex-M3",
                UsbVids = new List<string> { "0483" },
                UsbPids = new List<string> { "5740" },
                Pins = new Dictionary<int, PinInfo>(),
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface { Type = "SWD", ClockPin = 3, DataPin = 2, Voltage = 3.3 },
                    new ProgrammingInterface { Type = "ST-Link", ClockPin = 3, DataPin = 2, Voltage = 3.3 }
                }
            };
        if (!_database.ContainsKey("RP2040"))
            _database["RP2040"] = new ChipPinout
            {
                Name = "RP2040",
                Manufacturer = "Raspberry Pi",
                Description = "RP2040 Dual-Core ARM Cortex-M0+",
                UsbVids = new List<string> { "2E8A" },
                UsbPids = new List<string> { "000A" },
                Pins = new Dictionary<int, PinInfo>(),
                ProgrammingInterfaces = new List<ProgrammingInterface>
                {
                    new ProgrammingInterface { Type = "UART", DataPin = 0, Voltage = 3.3, Notes = "GP0=TX, GP1=RX" },
                    new ProgrammingInterface { Type = "SWD", ClockPin = 2, DataPin = 3, Voltage = 3.3 }
                }
            };
    }
    
    private static void SaveDatabase()
    {
        if (_database == null) return;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "pinouts.json");
            File.WriteAllText(path, JsonSerializer.Serialize(_database, new JsonSerializerOptions { WriteIndented = true }));
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
