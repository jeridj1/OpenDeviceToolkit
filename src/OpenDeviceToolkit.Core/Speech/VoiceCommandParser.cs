namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Parses voice input into structured commands.
/// </summary>
public static class VoiceCommandParser
{
    public static VoiceCommand Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new VoiceCommand { Type = VoiceCommandType.Unknown };
        
        input = input.ToLowerInvariant().Trim();
        
        // Remove wake words
        if (input.StartsWith("hey odt") || input.StartsWith("ok odt") || input.StartsWith("open device toolkit") || input.StartsWith("odt"))
        {
            var idx = input.IndexOfAny(new[] { ' ', '.' });
            if (idx >= 0) input = input.Substring(idx + 1).Trim();
        }
        
        if (Matches(input, "scan", "detect", "find", "discover", "look for"))
            return new VoiceCommand { Type = VoiceCommandType.ScanDevice, Device = Extract(input, "phone", "device", "hardware") };
        
        if (Matches(input, "root", "unlock", "gain access", "access", "control", "take over", "hack"))
            return new VoiceCommand { Type = VoiceCommandType.GainAccess, Device = Extract(input, "phone", "device"), Objective = ExtractObjective(input) ?? "gain full access" };
        
        if (Matches(input, "report", "generate", "create", "make", "save"))
            return new VoiceCommand { Type = VoiceCommandType.GenerateReport, Device = Extract(input, "phone", "device") };
        
        if (Matches(input, "reboot", "restart", "reset"))
            return new VoiceCommand { Type = VoiceCommandType.RebootDevice, Device = Extract(input, "phone", "device") };
        
        if (Matches(input, "exit", "quit", "close", "stop", "goodbye", "bye"))
            return new VoiceCommand { Type = VoiceCommandType.Exit };
        
        if (Matches(input, "help", "what can", "commands", "tell me", "how to"))
            return new VoiceCommand { Type = VoiceCommandType.Help };
        
        return new VoiceCommand { Type = VoiceCommandType.CustomObjective, Objective = input };
    }
    
    private static bool Matches(string input, params string[] keywords) => keywords.Any(k => input.Contains(k));
    
    private static string? Extract(string input, params string[] keywords)
    {
        foreach (var k in keywords)
        {
            var idx = input.IndexOf(k, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var after = input.Substring(idx + k.Length).Trim();
                if (!string.IsNullOrEmpty(after) && after.Length < 50)
                    return after;
            }
        }
        return null;
    }
    
    private static string? ExtractObjective(string input)
    {
        var actions = new[] { "to", "i want to", "i need to", "try to", "how to", "so that", "in order to" };
        foreach (var a in actions)
        {
            var idx = input.IndexOf(a, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var obj = input.Substring(idx + a.Length).Trim();
                if (!string.IsNullOrEmpty(obj)) return obj;
            }
        }
        return null;
    }
}

public enum VoiceCommandType
{
    Unknown, ScanDevice, GainAccess, GenerateReport, RebootDevice, CustomObjective, Help, Exit
}

public sealed record VoiceCommand
{
    public VoiceCommandType Type { get; init; } = VoiceCommandType.Unknown;
    public string? Device { get; init; }
    public string? Objective { get; init; }
}
