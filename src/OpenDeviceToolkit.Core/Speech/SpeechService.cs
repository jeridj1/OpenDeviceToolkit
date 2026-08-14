using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Provides speech-to-text and text-to-speech capabilities for hands-free interaction.
/// </summary>
public sealed class SpeechService : IDisposable
{
    private readonly SpeechRecognitionEngine _recognizer;
    private readonly SpeechSynthesizer _synthesizer;
    private readonly AppLogger? _logger;
    private bool _isListening = false;
    
    /// <summary>
    /// Event raised when text is recognized from speech.
    /// </summary>
    public event Action<string>? TextRecognized;
    
    /// <summary>
    /// Event raised when speech recognition starts.
    /// </summary>
    public event Action? ListeningStarted;
    
    /// <summary>
    /// Event raised when speech recognition stops.
    /// </summary>
    public event Action? ListeningStopped;
    
    /// <summary>
    /// Event raised when speech synthesis starts.
    /// </summary>
    public event Action? SpeakingStarted;
    
    /// <summary>
    /// Event raised when speech synthesis completes.
    /// </summary>
    public event Action? SpeakingCompleted;
    
    /// <summary>
    /// Gets or sets whether voice interaction is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the speech recognition confidence threshold (0.0 to 1.0).
    /// </summary>
    public float ConfidenceThreshold { get; set; } = 0.7f;
    
    /// <summary>
    /// Initializes a new speech service.
    /// </summary>
    public SpeechService(AppLogger? logger = null)
    {
        _logger = logger;
        
        try
        {
            // Initialize speech recognition
            _recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.SpeechHypothesized += OnSpeechHypothesized;
            _recognizer.RecognizeCompleted += OnRecognizeCompleted;
            
            // Load grammar (for better recognition of device-related terms)
            LoadCustomGrammar();
            
            // Initialize speech synthesizer
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SpeakStarted += (s, e) => SpeakingStarted?.Invoke();
            _synthesizer.SpeakCompleted += (s, e) => SpeakingCompleted?.Invoke();
            
            _logger?.Info("Speech service initialized");
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to initialize speech service", ex);
            throw;
        }
    }
    
    /// <summary>
    /// Loads custom grammar for better recognition of device-related terms.
    /// </summary>
    private void LoadCustomGrammar()
    {
        try
        {
            var grammar = new DictationGrammar();
            _recognizer.LoadGrammar(grammar);
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to load custom grammar: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Starts listening for speech input.
    /// </summary>
    public void StartListening()
    {
        if (!_isListening && IsEnabled)
        {
            try
            {
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;
                ListeningStarted?.Invoke();
                _logger?.Info("Speech recognition started");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to start listening", ex);
            }
        }
    }
    
    /// <summary>
    /// Stops listening for speech input.
    /// </summary>
    public void StopListening()
    {
        if (_isListening)
        {
            try
            {
                _recognizer.RecognizeAsyncStop();
                _isListening = false;
                ListeningStopped?.Invoke();
                _logger?.Info("Speech recognition stopped");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to stop listening", ex);
            }
        }
    }
    
    /// <summary>
    /// Toggles listening on/off.
    /// </summary>
    public void ToggleListening()
    {
        if (_isListening)
            StopListening();
        else
            StartListening();
    }
    
    /// <summary>
    /// Speaks the specified text.
    /// </summary>
    public void Speak(string text)
    {
        if (!IsEnabled)
            return;
        
        try
        {
            _synthesizer.SpeakAsync(text);
            _logger?.Info($"Speaking: {text}");
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to speak: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Speaks the specified text synchronously (blocks until complete).
    /// </summary>
    public void SpeakSync(string text)
    {
        if (!IsEnabled)
            return;
        
        try
        {
            _synthesizer.Speak(text);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to speak synchronously: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Cancels any ongoing speech.
    /// </summary>
    public void CancelSpeech()
    {
        try
        {
            _synthesizer.SpeakAsyncCancelAll();
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to cancel speech", ex);
        }
    }
    
    /// <summary>
    /// Gets or sets the speech rate (-10 to 10).
    /// </summary>
    public int Rate
    {
        get => _synthesizer.Rate;
        set => _synthesizer.Rate = Math.Clamp(value, -10, 10);
    }
    
    /// <summary>
    /// Gets or sets the speech volume (0 to 100).
    /// </summary>
    public int Volume
    {
        get => _synthesizer.Volume;
        set => _synthesizer.Volume = Math.Clamp(value, 0, 100);
    }
    
    /// <summary>
    /// Gets the current listening status.
    /// </summary>
    public bool IsListening => _isListening;
    
    /// <summary>
    /// Gets the current speaking status.
    /// </summary>
    public bool IsSpeaking => _synthesizer.State == SynthesizerState.Speaking;
    
    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result?.Confidence >= ConfidenceThreshold)
        {
            var text = e.Result.Text;
            _logger?.Info($"Recognized: {text} (Confidence: {e.Result.Confidence:P0})");
            TextRecognized?.Invoke(text);
        }
    }
    
    private void OnSpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e)
    {
        // Optional: Handle partial recognition
    }
    
    private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            _logger?.Error($"Recognition error: {e.Error.Message}");
        }
        else if (e.IsCompleted)
        {
            _logger?.Info("Recognition completed");
        }
    }
    
    public void Dispose()
    {
        StopListening();
        _recognizer?.Dispose();
        _synthesizer?.Dispose();
        _logger?.Info("Speech service disposed");
    }
}

/// <summary>
/// Provides voice command parsing for OpenDeviceToolkit.
/// </summary>
public static class VoiceCommandParser
{
    /// <summary>
    /// Parses voice input into a structured command.
    /// </summary>
    public static VoiceCommand? Parse(string input)
    {
        input = input.ToLowerInvariant().Trim();
        
        // Remove wake words
        if (input.StartsWith("hey odt") || input.StartsWith("ok odt") || input.StartsWith("open device toolkit"))
            input = input.Substring(input.IndexOfAny(new[] { ' ', '.' }) + 1).Trim();
        
        // Detect command type
        if (input.Contains("scan") || input.Contains("detect") || input.Contains("find"))
        {
            return new VoiceCommand
            {
                Type = VoiceCommandType.ScanDevice,
                Device = ExtractDevice(input),
                Objective = ExtractObjective(input)
            };
        }
        
        if (input.Contains("root") || input.Contains("unlock") || input.Contains("access") || input.Contains("control"))
        {
            return new VoiceCommand
            {
                Type = VoiceCommandType.GainAccess,
                Device = ExtractDevice(input),
                Objective = ExtractObjective(input) ?? "gain full access"
            };
        }
        
        if (input.Contains("report") || input.Contains("generate") || input.Contains("create"))
        {
            return new VoiceCommand
            {
                Type = VoiceCommandType.GenerateReport,
                Device = ExtractDevice(input)
            };
        }
        
        if (input.Contains("reboot") || input.Contains("restart"))
        {
            return new VoiceCommand
            {
                Type = VoiceCommandType.RebootDevice,
                Device = ExtractDevice(input)
            };
        }
        
        if (input.Contains("exit") || input.Contains("quit") || input.Contains("stop"))
        {
            return new VoiceCommand { Type = VoiceCommandType.Exit };
        }
        
        if (input.Contains("help") || input.Contains("what can"))
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
    
    private static string? ExtractDevice(string input)
    {
        // Look for common device identifiers
        var keywords = new[] { "phone", "device", "tablet", "board", "chip", "hardware" };
        
        foreach (var keyword in keywords)
        {
            var index = input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                // Extract the part after the keyword
                var after = input.Substring(index + keyword.Length).Trim();
                if (!string.IsNullOrEmpty(after))
                    return after;
            }
        }
        
        return null;
    }
    
    private static string? ExtractObjective(string input)
    {
        // Look for action words
        var actions = new[] { "to", "i want to", "i need to", "try to", "how to" };
        
        foreach (var action in actions)
        {
            var index = input.IndexOf(action, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return input.Substring(index + action.Length).Trim();
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
    public Dictionary<string, string>? Parameters { get; init; }
}
