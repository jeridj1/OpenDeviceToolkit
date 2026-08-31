namespace OpenDeviceToolkit.Core.Speech;

public static class VoiceCommandParser
{
    public static VoiceCommand Parse(string input) => VoiceIntentDetector.DetectIntent(input);
    public static VoiceCommand Parse(string input, VoiceContext? context) => VoiceIntentDetector.DetectIntent(input, context);
}
