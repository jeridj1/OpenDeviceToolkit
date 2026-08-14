namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Parses voice input into structured commands.
/// Legacy: Use VoiceIntentDetector for natural language parsing.
/// </summary>
public static class VoiceCommandParser
{
    public static VoiceCommand Parse(string input) => VoiceIntentDetector.DetectIntent(input);
    
    public static VoiceCommand Parse(string input, VoiceContext? context) => 
        VoiceIntentDetector.DetectIntent(input, context);
}
