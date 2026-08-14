using System.Text.Json.Serialization;

namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Settings for speech recognition and synthesis.
/// </summary>
public sealed class SpeechSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;
    
    [JsonPropertyName("rate")]
    public int Rate { get; set; } = 0;
    
    [JsonPropertyName("volume")]
    public int Volume { get; set; } = 100;
    
    [JsonPropertyName("confidenceThreshold")]
    public float ConfidenceThreshold { get; set; } = 0.7f;
    
    [JsonPropertyName("autoListen")]
    public bool AutoListen { get; set; } = false;
    
    [JsonPropertyName("voiceFeedback")]
    public bool VoiceFeedback { get; set; } = true;
}
