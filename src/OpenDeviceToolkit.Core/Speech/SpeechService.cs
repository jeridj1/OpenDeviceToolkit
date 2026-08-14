using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Provides speech-to-text and text-to-speech capabilities for hands-free interaction.
/// Enhanced with natural language understanding and context tracking.
/// </summary>
public sealed class SpeechService : IDisposable
{
    private readonly SpeechRecognitionEngine _recognizer;
    private readonly SpeechSynthesizer _synthesizer;
    private readonly AppLogger? _logger;
    private bool _isListening = false;
    private readonly VoiceContext _context = new();
    
    /// <summary>
    /// Event raised when text is recognized from speech.
    /// </summary>
    public event Action<string>? TextRecognized;
    
    /// <summary>
    /// Event raised when a command is detected (after intent parsing).
    /// </summary>
    public event Action<VoiceCommand>? CommandDetected;
    
    /// <summary>
    /// Event raised when clarification is needed.
    /// </summary>
    public event Action<ClarificationRequest>? ClarificationNeeded;
    
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
    public float ConfidenceThreshold { get; set; } = 0.6f;
    
    /// <summary>
    /// Gets the current voice context.
    /// </summary>
    public VoiceContext Context => _context;
    
    /// <summary>
    /// Initializes a new speech service.
    /// </summary>
    public SpeechService(AppLogger? logger = null)
    {
        _logger = logger;
        
        try
        {
            _recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.SpeechHypothesized += OnSpeechHypothesized;
            _recognizer.RecognizeCompleted += OnRecognizeCompleted;
            
            LoadCustomGrammar();
            
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SpeakStarted += (s, e) => SpeakingStarted?.Invoke();
            _synthesizer.SpeakCompleted += (s, e) => SpeakingCompleted?.Invoke();
            
            _logger?.Info("Speech service initialized with natural language support");
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to initialize speech service", ex);
            throw;
        }
    }
    
    private void LoadCustomGrammar()
    {
        try
        {
            // Use dictation grammar for free-form input
            var grammar = new DictationGrammar();
            _recognizer.LoadGrammar(grammar);
            
            // Add custom words for better recognition
            var customWords = new Choices("odt", "adb", "fastboot", "qualcomm", "snapdragon", "edl", "bootloader", "recovery", "rp2040", "pico", "stm32", "esp32");
            var customGrammar = new Grammar(new GrammarBuilder(customWords));
            _recognizer.LoadGrammar(customGrammar);
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to load custom grammar: {ex.Message}");
        }
    }
    
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
                Speak("Listening. How can I help?");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to start listening", ex);
            }
        }
    }
    
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
    
    public void ToggleListening()
    {
        if (_isListening)
            StopListening();
        else
            StartListening();
    }
    
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
    
    public int Rate
    {
        get => _synthesizer.Rate;
        set => _synthesizer.Rate = Math.Clamp(value, -10, 10);
    }
    
    public int Volume
    {
        get => _synthesizer.Volume;
        set => _synthesizer.Volume = Math.Clamp(value, 0, 100);
    }
    
    public bool IsListening => _isListening;
    public bool IsSpeaking => _synthesizer.State == SynthesizerState.Speaking;
    
    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result?.Confidence >= ConfidenceThreshold)
        {
            var text = e.Result.Text;
            _logger?.Info($"Recognized: {text} (Confidence: {e.Result.Confidence:P0})");
            
            // Parse intent with context
            var command = VoiceIntentDetector.DetectIntent(text, _context);
            
            // Update context
            _context.Update(command);
            
            // Check if we need clarification
            if (command.Type == VoiceCommandType.Unknown)
            {
                // Generate possible commands based on keywords
                var possibleCommands = GeneratePossibleCommands(text);
                if (possibleCommands.Count > 0)
                {
                    var request = ClarificationDialog.GenerateClarification(text, possibleCommands);
                    if (!request.IsResolved)
                    {
                        ClarificationNeeded?.Invoke(request);
                        Speak(request.Message);
                        return;
                    }
                    command = request.ResolvedCommand!;
                }
            }
            
            // Notify listeners
            TextRecognized?.Invoke(text);
            CommandDetected?.Invoke(command);
        }
    }
    
    private IReadOnlyList<VoiceCommand> GeneratePossibleCommands(string text)
    {
        var commands = new List<VoiceCommand>();
        var textLower = text.ToLower();
        
        // Check for device-related keywords
        if (textLower.Contains("phone") || textLower.Contains("device") || textLower.Contains("hardware"))
        {
            if (textLower.Contains("scan") || textLower.Contains("detect") || textLower.Contains("find"))
                commands.Add(new VoiceCommand { Type = VoiceCommandType.ScanDevice, Device = ExtractDeviceName(text) });
            
            if (textLower.Contains("unlock") || textLower.Contains("root") || textLower.Contains("access"))
                commands.Add(new VoiceCommand { Type = VoiceCommandType.GainAccess, Device = ExtractDeviceName(text) });
            
            if (textLower.Contains("reboot") || textLower.Contains("restart"))
                commands.Add(new VoiceCommand { Type = VoiceCommandType.RebootDevice, Device = ExtractDeviceName(text) });
        }
        
        if (textLower.Contains("report") || textLower.Contains("log") || textLower.Contains("document"))
            commands.Add(new VoiceCommand { Type = VoiceCommandType.GenerateReport });
        
        if (textLower.Contains("help") || textLower.Contains("what") || textLower.Contains("how"))
            commands.Add(new VoiceCommand { Type = VoiceCommandType.Help });
        
        if (textLower.Contains("exit") || textLower.Contains("quit") || textLower.Contains("close"))
            commands.Add(new VoiceCommand { Type = VoiceCommandType.Exit });
        
        return commands;
    }
    
    private string? ExtractDeviceName(string text)
    {
        var keywords = new[] { "phone", "device", "hardware", "board", "chip" };
        foreach (var keyword in keywords)
        {
            var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && index + keyword.Length < text.Length)
            {
                var after = text.Substring(index + keyword.Length).Trim();
                if (!string.IsNullOrEmpty(after))
                {
                    // Get the next word or few words
                    var words = after.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 0)
                        return words[0];
                }
            }
        }
        return null;
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
