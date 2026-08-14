namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Interface for speech services.
/// </summary>
public interface ISpeechService : IDisposable
{
    /// <summary>
    /// Event raised when text is recognized from speech.
    /// </summary>
    event Action<string>? TextRecognized;
    
    /// <summary>
    /// Event raised when speech recognition starts.
    /// </summary>
    event Action? ListeningStarted;
    
    /// <summary>
    /// Event raised when speech recognition stops.
    /// </summary>
    event Action? ListeningStopped;
    
    /// <summary>
    /// Gets or sets whether voice interaction is enabled.
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets whether the service is currently listening.
    /// </summary>
    bool IsListening { get; }
    
    /// <summary>
    /// Gets whether the service is currently speaking.
    /// </summary>
    bool IsSpeaking { get; }
    
    /// <summary>
    /// Starts listening for speech input.
    /// </summary>
    void StartListening();
    
    /// <summary>
    /// Stops listening for speech input.
    /// </summary>
    void StopListening();
    
    /// <summary>
    /// Toggles listening on/off.
    /// </summary>
    void ToggleListening();
    
    /// <summary>
    /// Speaks the specified text asynchronously.
    /// </summary>
    void Speak(string text);
    
    /// <summary>
    /// Speaks the specified text synchronously.
    /// </summary>
    void SpeakSync(string text);
    
    /// <summary>
    /// Cancels any ongoing speech.
    /// </summary>
    void CancelSpeech();
}
