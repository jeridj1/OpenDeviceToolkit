using System.Text.RegularExpressions;

namespace OpenDeviceToolkit.Core.Speech;

public static class VoiceIntentDetector
{
    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["phone"] = new[] { "device", "hardware", "gadget", "thing", "board", "chip", "unit" },
        ["lg"] = new[] { "lg phone", "v50", "v40", "v60", "g8", "g7", "stylo" },
        ["samsung"] = new[] { "galaxy", "note" },
        ["pixel"] = new[] { "google phone" },
        ["scan"] = new[] { "detect", "find", "look for", "check", "see", "what's", "discover", "identify" },
        ["unlock"] = new[] { "root", "gain access", "bypass", "hack", "crack", "open", "access", "control", "jailbreak" },
        ["generate"] = new[] { "create", "make", "build", "produce", "write", "save" },
        ["report"] = new[] { "log", "file", "document", "summary", "info", "details" },
        ["reboot"] = new[] { "restart", "reset", "power cycle", "turn off and on" },
        ["help"] = new[] { "what can", "commands", "tell me", "how to", "how do", "options", "list" },
        ["exit"] = new[] { "quit", "close", "stop", "goodbye", "bye", "shut down", "end" },
        ["usb"] = new[] { "usb cable", "usb port" },
        ["connected"] = new[] { "plugged in", "attached", "hooked up", "linked" },
        ["locked"] = new[] { "unlocked", "secured", "protected" },
        ["bootloader"] = new[] { "fastboot", "download mode", "edl mode", "9008 mode" },
        ["edl"] = new[] { "emergency download", "9008", "qualcomm mode" },
        ["qualcomm"] = new[] { "qc", "snapdragon", "sd" },
        ["rp2040"] = new[] { "pico", "raspberry pi pico" },
        ["programmer"] = new[] { "flasher", "writer", "burner" },
        ["please"] = new[] { "can you", "could you", "would you" }
    };
    
    private static readonly Dictionary<string, VoiceCommandType> IntentKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["scan"] = VoiceCommandType.ScanDevice,
        ["detect"] = VoiceCommandType.ScanDevice,
        ["find"] = VoiceCommandType.ScanDevice,
        ["check"] = VoiceCommandType.ScanDevice,
        ["unlock"] = VoiceCommandType.GainAccess,
        ["root"] = VoiceCommandType.GainAccess,
        ["gain"] = VoiceCommandType.GainAccess,
        ["access"] = VoiceCommandType.GainAccess,
        ["generate"] = VoiceCommandType.GenerateReport,
        ["create"] = VoiceCommandType.GenerateReport,
        ["make"] = VoiceCommandType.GenerateReport,
        ["report"] = VoiceCommandType.GenerateReport,
        ["reboot"] = VoiceCommandType.RebootDevice,
        ["restart"] = VoiceCommandType.RebootDevice,
        ["help"] = VoiceCommandType.Help,
        ["exit"] = VoiceCommandType.Exit,
        ["quit"] = VoiceCommandType.Exit
    };
    
    private static readonly Regex WakeWordPattern = new(@"\b(hey odt|ok odt|open device toolkit|odt)\b", RegexOptions.IgnoreCase);
    
    public static VoiceCommand DetectIntent(string input, VoiceContext? context = null)
    {
        context ??= new VoiceContext();
        if (string.IsNullOrWhiteSpace(input)) return new VoiceCommand { Type = VoiceCommandType.Unknown };
        input = CleanInput(input);
        
        var directMatch = MatchDirectIntent(input);
        if (directMatch.Type != VoiceCommandType.Unknown)
            return directMatch with { Device = ExtractDevice(input, context) ?? directMatch.Device, Objective = ExtractObjective(input, context) ?? directMatch.Objective };
        
        var fuzzyMatch = MatchFuzzyIntent(input);
        if (fuzzyMatch.Type != VoiceCommandType.Unknown)
            return fuzzyMatch with { Device = ExtractDevice(input, context), Objective = ExtractObjective(input, context) };
        
        var contextMatch = InferFromContext(input, context);
        if (contextMatch.Type != VoiceCommandType.Unknown) return contextMatch;
        
        return new VoiceCommand { Type = VoiceCommandType.CustomObjective, Objective = input, Device = ExtractDevice(input, context) };
    }
    
    private static string CleanInput(string input)
    {
        input = WakeWordPattern.Replace(input, "");
        input = input.Replace("please", "").Replace("can you", "").Replace("could you", "")
                      .Replace("would you", "").Replace("i want to", "").Replace("i need to", "")
                      .Replace("try to", "").Replace("how to", "").Replace("so that", "")
                      .Replace("in order to", "");
        return input.Trim().Replace("  ", " ");
    }
    
    private static VoiceCommand MatchDirectIntent(string input)
    {
        foreach (var kvp in IntentKeywords)
            if (input.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return new VoiceCommand { Type = kvp.Value };
        return new VoiceCommand { Type = VoiceCommandType.Unknown };
    }
    
    private static VoiceCommand MatchFuzzyIntent(string input)
    {
        var inputWords = input.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var kvp in IntentKeywords)
        {
            if (inputWords.Any(w => w.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))) return new VoiceCommand { Type = kvp.Value };
            if (Synonyms.TryGetValue(kvp.Key, out var synonyms) && inputWords.Any(w => synonyms.Contains(w, StringComparer.OrdinalIgnoreCase))) return new VoiceCommand { Type = kvp.Value };
        }
        return new VoiceCommand { Type = VoiceCommandType.Unknown };
    }
    
    private static VoiceCommand InferFromContext(string input, VoiceContext context)
    {
        if (!string.IsNullOrEmpty(context.CurrentDevice))
        {
            if (input.Contains("unlock", StringComparison.OrdinalIgnoreCase) || input.Contains("root", StringComparison.OrdinalIgnoreCase))
                return new VoiceCommand { Type = VoiceCommandType.GainAccess, Device = context.CurrentDevice, Objective = $"Gain access to {context.CurrentDevice}" };
            if (input.Contains("scan", StringComparison.OrdinalIgnoreCase))
                return new VoiceCommand { Type = VoiceCommandType.ScanDevice, Device = context.CurrentDevice };
        }
        if (IsDeviceDescription(input)) return new VoiceCommand { Type = VoiceCommandType.ScanDevice, Device = input };
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
        if (!string.IsNullOrEmpty(context.CurrentDevice)) return context.CurrentDevice;
        var deviceKeywords = new[] { "phone", "device", "board", "chip", "hardware", "gadget", "lg", "samsung", "pixel", "qualcomm" };
        foreach (var keyword in deviceKeywords)
        {
            var index = input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var after = input.Substring(index + keyword.Length).Trim();
                if (!string.IsNullOrEmpty(after) && after.Length < 50)
                {
                    after = after.Split(new[] { ' ', '.', ',', '!', '?' })[0];
                    if (!string.IsNullOrEmpty(after)) return after;
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
                    if (!string.IsNullOrEmpty(objective)) return objective;
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
    public void Reset() { CurrentDevice = null; CurrentObjective = null; LastAction = null; SessionStart = DateTime.UtcNow; }
    public void Update(VoiceCommand command) { if (command.Device != null) CurrentDevice = command.Device; if (command.Objective != null) CurrentObjective = command.Objective; LastAction = command.Type; }
}
