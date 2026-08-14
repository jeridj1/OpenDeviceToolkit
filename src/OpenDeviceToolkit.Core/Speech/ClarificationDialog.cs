namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Handles clarification when voice input is ambiguous.
/// </summary>
public static class ClarificationDialog
{
    /// <summary>
    /// Generates clarification options when input is ambiguous.
    /// </summary>
    public static ClarificationRequest GenerateClarification(string input, IReadOnlyList<VoiceCommand> possibleCommands)
    {
        if (possibleCommands.Count == 0)
            return new ClarificationRequest(false, "I didn't understand that. Could you rephrase?");
        
        if (possibleCommands.Count == 1)
            return new ClarificationRequest(true, possibleCommands[0]);
        
        // Multiple possible commands - ask for clarification
        var options = possibleCommands.Select(c => c.Type.GetDescription()).ToList();
        return new ClarificationRequest(
            false,
            $"Did you mean:\r\n1. {options[0]}\r\n2. {options[1]}\r\n{(options.Count > 2 ? $"3. {options[2]}\r\n" : "")}Please say the number."
        );
    }
    
    /// <summary>
    /// Parses clarification response (e.g., "1", "two", "the first one").
    /// </summary>
    public static int? ParseClarificationResponse(string response, int maxOptions)
    {
        // Try numeric
        if (int.TryParse(response, out int number) && number >= 1 && number <= maxOptions)
            return number;
        
        // Try word numbers
        var wordToNumber = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
            ["first"] = 1, ["second"] = 2, ["third"] = 3, ["fourth"] = 4, ["fifth"] = 5
        };
        
        if (wordToNumber.TryGetValue(response.Trim(), out int wordNumber) && wordNumber <= maxOptions)
            return wordNumber;
        
        return null;
    }
}

/// <summary>
/// Represents a request for clarification from the user.
/// </summary>
public sealed record ClarificationRequest(bool IsResolved, string Message, VoiceCommand? ResolvedCommand = null);

/// <summary>
/// Extension methods for VoiceCommandType.
/// </summary>
public static class VoiceCommandTypeExtensions
{
    public static string GetDescription(this VoiceCommandType type) => type switch
    {
        VoiceCommandType.Unknown => "Unknown command",
        VoiceCommandType.ScanDevice => "Scan for connected devices",
        VoiceCommandType.GainAccess => "Gain access or unlock device",
        VoiceCommandType.GenerateReport => "Generate a diagnostic report",
        VoiceCommandType.RebootDevice => "Reboot the device",
        VoiceCommandType.CustomObjective => "Custom objective",
        VoiceCommandType.Help => "Show help and available commands",
        VoiceCommandType.Exit => "Exit the application",
        _ => "Unknown command"
    };
}
