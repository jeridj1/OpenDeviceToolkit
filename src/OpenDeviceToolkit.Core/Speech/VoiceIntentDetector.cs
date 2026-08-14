using System.Text.RegularExpressions;

namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Detects user intent from natural language voice input.
/// Uses multi-layer approach: direct matching → fuzzy matching → context → clarification.
/// </summary>
public static class VoiceIntentDetector
{
    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Devices
        ["phone"] = new[] { "device", "hardware", "gadget", "thing", "board", "chip", "unit", "doodad" },
        ["lg"] = new[] { "lg phone", "lg device", "v50", "v40", "v60", "g8", "g7", "stylo" },
        ["samsung"] = new[] { "galaxy", "note", "s series", "a series" },
        ["pixel"] = new[] { "google phone", "google device" },
        
        // Actions: Scan/Detect
        ["scan"] = new[] { "detect", "find", "look for", "check", "see", "what's", "discover", "identify" },
        ["device"] = new[] { "phone", "hardware", "gadget", "thing", "board", "chip" },
        
        // Actions: Access/Unlock
        ["unlock"] = new[] { "root", "gain access", "bypass", "hack", "crack", "open", "access", "control", "take over", "jailbreak" },
        ["root"] = new[] { "unlock", "gain access", "bypass", "hack", "crack", "open", "access" },
        ["gain access"] = new[] { "unlock", "root", "bypass", "hack", "crack", "open", "access", "control" },
        
        // Actions: Generate
        ["generate"] = new[] { "create", "make", "build", "produce", "write", "save" },
        ["report"] = new[] { "log", "file", "document", "summary", "info", "details" },
        
        // Actions: Reboot
        ["reboot"] = new[] { "restart", "reset", "power cycle", "turn off and on", "reboot device", "reboot phone" },
        ["restart"] = new[] { "reboot", "reset", "power cycle" },
        
        // Actions: Help
        ["help"] = new[] { "what can", "commands", "tell me", "how to", "how do", "options", "list" },
        
        // Actions: Exit
        ["exit"] = new[] { "quit", "close", "stop", "goodbye", "bye", "shut down", "end" },
        
        // Connectivity
        ["usb"] = new[] { "usb cable", "usb port", "usb connection" },
        ["connected"] = new[] { "plugged in", "attached", "hooked up", "linked" },
        
        // States
        ["locked"] = new[] { "unlocked", "secured", "protected" },
        ["bootloader"] = new[] { "boot loader", "fastboot", "download mode", "edl mode", "9008 mode" },
        ["recovery"] = new[] { "recovery mode", "stock recovery", "custom recovery", "twrp" },
        
        // Qualcomm specific
        ["edl"] = new[] { "emergency download", "9008", "qualcomm mode" },
        ["qc"] = new[] { "qualcomm" },
        ["snapdragon"] = new[] { "sd", "soc" },
        
        // Tools
        ["rp2040"] = new[] { "pico", "raspberry pi pico", "rp2040 board" },
        ["programmer"] = new[] { "flasher", "writer", "burner" },
        ["logic analyzer"] = new[] { "logic", "analyzer", "sniffer", "scope" },
        
        // General
        ["please"] = new[] { "can you", "could you", "would you", "i need", "i want" },
        ["the"] = new[] { "this", "that", "my", "a", "an" },
        ["to"] = new[] { "2", "too" },
    };
    
    private static readonly Dictionary<string, VoiceCommandType> IntentKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["scan"] = VoiceCommandType.ScanDevice,
        ["detect"] = VoiceCommandType.ScanDevice,
        ["find"] = VoiceCommandType.ScanDevice,
        ["check"] = VoiceCommandType.ScanDevice,
        ["look"] = VoiceCommandType.ScanDevice,
        ["what"] = VoiceCommandType.ScanDevice,
        ["see"] = VoiceCommandType.ScanDevice,
        ["discover"] = VoiceCommandType.ScanDevice,
        ["identify"] = VoiceCommandType.ScanDevice,
        ["unlock"] = VoiceCommandType.GainAccess,
        ["root"] = VoiceCommandType.GainAccess,
        ["gain"] = VoiceCommandType.GainAccess,
        ["access"] = VoiceCommandType.GainAccess,
        ["bypass"] = VoiceCommandType.GainAccess,
        ["hack"] = VoiceCommandType.GainAccess,
        ["crack"] = VoiceCommandType.GainAccess,
        ["control"] = VoiceCommandType.GainAccess,
        ["jailbreak"] = VoiceCommandType.GainAccess,
        ["exploit"] = VoiceCommandType.GainAccess,
        ["generate"] = VoiceCommandType.GenerateReport,
        ["create"] = VoiceCommandType.GenerateReport,
        ["make"] = VoiceCommandType.GenerateReport,
        ["report"] = VoiceCommandType.GenerateReport,
        ["log"] = VoiceCommandType.GenerateReport,
        ["save"] = VoiceCommandType.GenerateReport,
        ["document"] = VoiceCommandType.GenerateReport,
        ["reboot"] = VoiceCommandType.RebootDevice,
        ["restart"] = VoiceCommandType.RebootDevice,
        ["reset"] = VoiceCommandType.RebootDevice,
        ["power"] = VoiceCommandType.RebootDevice,
        ["help"] = VoiceCommandType.Help,
        ["what"] = VoiceCommandType.Help,
        ["commands"] = VoiceCommandType.Help,
        ["how"] = VoiceCommandType.Help,
        ["list"] = VoiceCommandType.Help,
        ["exit"] = VoiceCommandType.Exit,
        ["quit"] = VoiceCommandType.Exit,
        ["close"] = VoiceCommandType.Exit,
        ["stop"] = VoiceCommandType.Exit,
        ["goodbye"] = VoiceCommandType.Exit,
        ["bye"] = VoiceCommandType.Exit,
    };
    
    private static readonly Regex WakeWordPattern = new(@"\b(hey odt|ok odt|open device toolkit|odt|hey computer)\b", RegexOptions.IgnoreCase);
    
    /// <summary>
    /// Detects intent from natural language input with context.
    /// </summary>
    public static VoiceCommand DetectIntent(string input, VoiceContext? context = null)
    {
        context ??= new VoiceContext();
        
        if (string.IsNullOrWhiteSpace(input))
            return new VoiceCommand { Type = VoiceCommandType.Unknown };
        
        // Clean up input
        input = CleanInput(input);
        
        // Layer 1: Direct intent matching
        var directMatch = MatchDirectIntent(input);
        if (directMatch.Type != VoiceCommandType.Unknown)
            return directMatch with { Device = ExtractDevice(input, context) ?? directMatch.Device, Objective = ExtractObjective(input, context) ?? directMatch.Objective };
        
        // Layer 2: Fuzzy matching with synonyms
        var fuzzyMatch = MatchFuzzyIntent(input);
        if (fuzzyMatch.Type != VoiceCommandType.Unknown)
            return fuzzyMatch with { Device = ExtractDevice(input, context), Objective = ExtractObjective(input, context) };
        
        // Layer 3: Context-based inference
        var contextMatch = InferFromContext(input, context);
        if (contextMatch.Type != VoiceCommandType.Unknown)
            return contextMatch;
        
        // Layer 4: Free-form objective
        return new VoiceCommand
        {
            Type = VoiceCommandType.CustomObjective,
            Objective = input,
            Device = ExtractDevice(input, context)
        };
    }
    
    private static string CleanInput(string input)
    {
        input = WakeWordPattern.Replace(input, "");
        input = input.Replace("please", "")
                      .Replace("can you", "")
                      .Replace("could you", "")
                      .Replace("would you", "")
                      .Replace("i want to", "")
                      .Replace("i need to", "")
                      .Replace("try to", "")
                      .Replace("how to", "")
                      .Replace("so that", "")
                      .Replace("in order to", "");
        return input.Trim().Replace("  ", " ");
    }
    
    private static VoiceCommand MatchDirectIntent(string input)
    {
        foreach (var kvp in IntentKeywords)
        {
            if (input.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return new VoiceCommand { Type = kvp.Value };
        }
        return new VoiceCommand { Type = VoiceCommandType.Unknown };
    }
    
    private static VoiceCommand MatchFuzzyIntent(string input)
    {
        var inputWords = input.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var kvp in IntentKeywords)
        {
            if (inputWords.Any(w => w.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)))
                return new VoiceCommand { Type = kvp.Value };
            if (Synonyms.TryGetValue(kvp.Key, out var synonyms) && inputWords.Any(w => synonyms.Contains(w, StringComparer.OrdinalIgnoreCase)))
                return new VoiceCommand { Type = kvp.Value };
        }
        return new VoiceCommand { Type = VoiceCommandType.Unknown };
    }
    
    private static VoiceCommand InferFromContext(string input, VoiceContext context)
    {
        if (!string.IsNullOrEmpty(context.CurrentDevice))
        {
            if (input.Contains("unlock", StringComparison.OrdinalIgnoreCase) || input.Contains("root", StringComparison.OrdinalIgnoreCase) || input.Contains("access", StringComparison.OrdinalIgnoreCase))
                return new VoiceCommand { Type = VoiceCommandType.GainAccess, Device = context.CurrentDevice, Objective = $"Gain access to {context.CurrentDevice}" };
            if (input.Contains("scan", StringComparison.OrdinalIgnoreCase) || input.Contains("check", StringComparison.OrdinalIgnoreCase))
                return new VoiceCommand { Type = VoiceCommandType.ScanDevice, Device = context.CurrentDevice };
        }
        if (IsDeviceDescription(input))
            return new VoiceCommand { Type = VoiceCommandType.ScanDevice, Device = input };
        return new VoiceCommand { Type = VoiceCommandType.Unknown };
    }
    
    private static bool IsDeviceDescription(string input)
    {
        var deviceKeywords = new[] { "phone", "device", "board", "chip", "hardware", "gadget" };
        var hasDeviceKeyword = deviceKeywords.Any(k => input.Contains(k, StringComparison.OrdinalIgnoreCase));
        var actionKeywords = IntentKeywords.Keys.ToArray();
        var hasActionKeyword = actionKeywords.Any(k => input.Contains(k, StringComparison.OrdinalIgnoreCase));
        return hasDeviceKeyword && !hasActionKeyword;
    }
    
    private static string? ExtractDevice(string input, VoiceContext context)
    {
        if (!string.IsNullOrEmpty(context.CurrentDevice))
            return context.CurrentDevice;
        var deviceKeywords = new[] { "phone", "device", "board", "chip", "hardware", "gadget", "lg", "samsung", "pixel", "qualcomm", "stm32", "esp32", "arduino", "raspberry" };
        foreach (var keyword in deviceKeywords)
        {
            var index = input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var after = input.Substring(index + keyword.Length).Trim();
                if (!string.IsNullOrEmpty(after) && after.Length < 50)
                {
                    after = after.Split(new[] { ' ', '.', ',', '!', '?' })[0];
                    if (!string.IsNullOrEmpty(after))
                        return after;
                }
                return keyword;
            }
        }
        return null;
    }
    
    private static string? ExtractObjective(string input, VoiceContext context)
    {
        var objectiveMarkers = new[] { "to", "i want to", "i need to", "try to", "how to", "so that", "in order to" };
        foreach (var marker in objectiveMarkers)
        {
            var index = input.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var objective = input.Substring(index + marker.Length).Trim();
                if (!string.IsNullOrEmpty(objective))
                {
                    objective = objective.Split(new[] { '.', ',', '!', '?' })[0].Trim();
                    if (!string.IsNullOrEmpty(objective))
                        return objective;
                }
            }
        }
        if (!string.IsNullOrEmpty(context.CurrentDevice) && input.Contains(context.CurrentDevice, StringComparison.OrdinalIgnoreCase))
            return input.Replace(context.CurrentDevice, "", StringComparison.OrdinalIgnoreCase).Trim();
        return null;
    }
}

public sealed class VoiceContext
{
    public string? CurrentDevice { get; set; }
    public string? CurrentObjective { get; set; }
    public VoiceCommandType? LastAction { get; set; }
    public DateTime SessionStart { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration => DateTime.UtcNow - SessionStart;
    
    public void Reset()
    {
        CurrentDevice = null;
        CurrentObjective = null;
        LastAction = null;
        SessionStart = DateTime.UtcNow;
    }
    
    public void Update(VoiceCommand command)
    {
        if (command.Device != null) CurrentDevice = command.Device;
        if (command.Objective != null) CurrentObjective = command.Objective;
        LastAction = command.Type;
    }
}
