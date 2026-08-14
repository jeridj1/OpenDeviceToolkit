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
    
    public event Action<string>? TextRecognized;
    public event Action? ListeningStarted;
    public event Action? ListeningStopped;
    public event Action? SpeakingStarted;
    public event Action? SpeakingCompleted;
    
    public bool IsEnabled { get; set; } = true;
    public float ConfidenceThreshold { get; set; } = 0.7f;
    
    public SpeechService(AppLogger? logger = null)
    {
        _logger = logger;
        
        try
        {
            _recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.RecognizeCompleted += OnRecognizeCompleted;
            
            LoadCustomGrammar();
            
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
            TextRecognized?.Invoke(text);
        }
    }
    
    private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            _logger?.Error($"Recognition error: {e.Error.Message}");
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
