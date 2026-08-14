namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Parses voice input into structured commands for OpenDeviceToolkit.
/// </summary>
public static class VoiceCommandParser
{
    /// <summary>
    /// Parses voice input into a structured command.
    /// </summary>
    public static VoiceCommand Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new VoiceCommand { Type = VoiceCommandType.Unknown };
        
        input = input.ToLowerInvariant().Trim();
        
        // Remove wake words
        if (input.StartsWith("hey odt") || input.StartsWith("ok odt") || 
            input.StartsWith("open device toolkit") || input.StartsWith("odt"))
        {
            var wakeIndex = input.IndexOfAny(new[] { ' ', '.' });
            if (wakeIndex >= 0)
                input = input.Substring(wakeIndex + 1).Trim();
        }
        
        // Detect command type
        if (MatchesAny(input, "scan", "detect", "find", "discover", "look for"))
        {
            return new VoiceCommand
            {
                Type = VoiceCommandType.ScanDevice,
                Device = ExtractDevice(input),
                Objective = ExtractObjective(input)
            };
        }
        
        if (MatchesAny(input, "root", "unlock", "gain access", "access", "control", "take over", "hack"))
        {
            return new VoiceCommand
            {
                Type = VoiceCommandType.GainAccess,
                Device = ExtractDevice(input),
                Objective = ExtractObjective(input) ?? "gain full access"
            };
        }
        
        if (MatchesAny(input, "report", "generate", "create", "make", "save"))
        {
            return new VoiceCommand
            {
                Type = VoiceCommandType.GenerateReport,
                Device = ExtractDevice(input)
            };
        }
        
        if (MatchesAny(input, "reboot", "restart", "reboot device", "reset"))
        {
            return new VoiceCommand
            {
                Type = VoiceCommandType.RebootDevice,
                Device = ExtractDevice(input)
            };
        }
        
        if (MatchesAny(input, "exit", "quit", "close", "stop", "goodbye", "bye"))
        {
            return new VoiceCommand { Type = VoiceCommandType.Exit };
        }
        
        if (MatchesAny(input, "help", "what can", "commands", "tell me", "how to"))
        {
            return new VoiceCommand { Type = VoiceCommandType.Help };
        }
        
        // Try to extract as a general objective
        return new VoiceCommand
        {
            Type = VoiceCommandType.CustomObjective,
            Objective = input
        };
    }
    
    private static bool MatchesAny(string input, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (input.Contains(keyword))
                return true;
        }
        return false;
    }
    
    private static string? ExtractDevice(string input)
    {
        var keywords = new[] { "phone", "device", "tablet", "board", "chip", "hardware", "on my", "the" };
        
        foreach (var keyword in keywords)
        {
            var index = input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var after = input.Substring(index + keyword.Length).Trim();
                if (!string.IsNullOrEmpty(after) && after.Length < 50)
                    return after;
            }
        }
        
        return null;
    }
    
    private static string? ExtractObjective(string input)
    {
        var actions = new[] { "to", "i want to", "i need to", "try to", "how to", "so that", "in order to" };
        
        foreach (var action in actions)
        {
            var index = input.IndexOf(action, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var objective = input.Substring(index + action.Length).Trim();
                if (!string.IsNullOrEmpty(objective))
                    return objective;
            }
        }
        
        return null;
    }
}

/// <summary>
/// Types of voice commands.
/// </summary>
public enum VoiceCommandType
{
    Unknown,
    ScanDevice,
    GainAccess,
    GenerateReport,
    RebootDevice,
    CustomObjective,
    Help,
    Exit
}

/// <summary>
/// Represents a parsed voice command.
/// </summary>
public sealed record VoiceCommand
{
    public VoiceCommandType Type { get; init; } = VoiceCommandType.Unknown;
    public string? Device { get; init; }
    public string? Objective { get; init; }
}
