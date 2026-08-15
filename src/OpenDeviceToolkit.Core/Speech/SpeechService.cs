using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace OpenDeviceToolkit.Core.Speech;

public sealed class SpeechService : IDisposable
{
    private readonly SpeechRecognitionEngine _recognizer;
    private readonly SpeechSynthesizer _synthesizer;
    private readonly AppLogger? _logger;
    private bool _isListening = false;
    private readonly VoiceContext _context = new();
    
    public event Action<string>? TextRecognized;
    public event Action<VoiceCommand>? CommandDetected;
    public event Action<ClarificationRequest>? ClarificationNeeded;
    public event Action? ListeningStarted;
    public event Action? ListeningStopped;
    public event Action? SpeakingStarted;
    public event Action? SpeakingCompleted;
    
    public bool IsEnabled { get; set; } = true;
    public float ConfidenceThreshold { get; set; } = 0.6f;
    public VoiceContext Context => _context;
    
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
        catch (Exception ex) { _logger?.Error("Failed to initialize speech service", ex); throw; }
    }
    
    private void LoadCustomGrammar()
    {
        try
        {
            var grammar = new DictationGrammar();
            _recognizer.LoadGrammar(grammar);
            var customWords = new Choices("odt", "adb", "fastboot", "qualcomm", "snapdragon", "edl", "bootloader", "recovery", "rp2040", "pico", "stm32", "esp32");
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(customWords)));
        }
        catch (Exception ex) { _logger?.Warning($"Failed to load custom grammar: {ex.Message}"); }
    }
    
    public void StartListening()
    {
        if (!_isListening && IsEnabled)
        {
            try { _recognizer.RecognizeAsync(RecognizeMode.Multiple); _isListening = true; ListeningStarted?.Invoke(); _logger?.Info("Speech recognition started"); Speak("Listening. How can I help?"); }
            catch (Exception ex) { _logger?.Error("Failed to start listening", ex); }
        }
    }
    
    public void StopListening()
    {
        if (_isListening)
        {
            try { _recognizer.RecognizeAsyncStop(); _isListening = false; ListeningStopped?.Invoke(); _logger?.Info("Speech recognition stopped"); }
            catch (Exception ex) { _logger?.Error("Failed to stop listening", ex); }
        }
    }
    
    public void ToggleListening()
    {
        if (_isListening) StopListening(); else StartListening();
    }
    
    public void Speak(string text)
    {
        if (!IsEnabled) return;
        try { _synthesizer.SpeakAsync(text); _logger?.Info($"Speaking: {text}"); }
        catch (Exception ex) { _logger?.Error($"Failed to speak: {ex.Message}", ex); }
    }
    
    public void SpeakSync(string text)
    {
        if (!IsEnabled) return;
        try { _synthesizer.Speak(text); }
        catch (Exception ex) { _logger?.Error($"Failed to speak: {ex.Message}", ex); }
    }
    
    public void CancelSpeech() => _synthesizer.SpeakAsyncCancelAll();
    
    public int Rate { get => _synthesizer.Rate; set => _synthesizer.Rate = Math.Clamp(value, -10, 10); }
    public int Volume { get => _synthesizer.Volume; set => _synthesizer.Volume = Math.Clamp(value, 0, 100); }
    public bool IsListening => _isListening;
    public bool IsSpeaking => _synthesizer.State == SynthesizerState.Speaking;
    
    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result?.Confidence >= ConfidenceThreshold)
        {
            var text = e.Result.Text;
            _logger?.Info($"Recognized: {text} (Confidence: {e.Result.Confidence:P0})");
            var command = VoiceIntentDetector.DetectIntent(text, _context);
            _context.Update(command);
            if (command.Type == VoiceCommandType.Unknown)
            {
                var possibleCommands = GeneratePossibleCommands(text);
                if (possibleCommands.Count > 0)
                {
                    var request = ClarificationDialog.GenerateClarification(text, possibleCommands);
                    if (!request.IsResolved) { ClarificationNeeded?.Invoke(request); Speak(request.Message); return; }
                    command = request.ResolvedCommand!;
                }
            }
            TextRecognized?.Invoke(text);
            CommandDetected?.Invoke(command);
        }
    }
    
    private IReadOnlyList<VoiceCommand> GeneratePossibleCommands(string text)
    {
        var commands = new List<VoiceCommand>();
        var t = text.ToLower();
        if (t.Contains("phone") || t.Contains("device"))
        {
            if (t.Contains("scan") || t.Contains("detect")) commands.Add(new VoiceCommand { Type = VoiceCommandType.ScanDevice, Device = ExtractDeviceName(t) });
            if (t.Contains("unlock") || t.Contains("root")) commands.Add(new VoiceCommand { Type = VoiceCommandType.GainAccess, Device = ExtractDeviceName(t) });
            if (t.Contains("reboot")) commands.Add(new VoiceCommand { Type = VoiceCommandType.RebootDevice, Device = ExtractDeviceName(t) });
        }
        if (t.Contains("report")) commands.Add(new VoiceCommand { Type = VoiceCommandType.GenerateReport });
        if (t.Contains("help")) commands.Add(new VoiceCommand { Type = VoiceCommandType.Help });
        if (t.Contains("exit") || t.Contains("quit")) commands.Add(new VoiceCommand { Type = VoiceCommandType.Exit });
        return commands;
    }
    
    private string? ExtractDeviceName(string text)
    {
        var keywords = new[] { "phone", "device", "hardware", "board", "chip" };
        foreach (var k in keywords)
        {
            var idx = text.IndexOf(k);
            if (idx >= 0 && idx + k.Length < text.Length)
            {
                var after = text.Substring(idx + k.Length).Trim();
                if (!string.IsNullOrEmpty(after)) return after.Split(' ', '.', ',')[0];
            }
        }
        return null;
    }
    
    private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        if (e.Error != null) _logger?.Error($"Recognition error: {e.Error.Message}");
    }
    
    public void Dispose() { StopListening(); _recognizer?.Dispose(); _synthesizer?.Dispose(); _logger?.Info("Speech service disposed"); }
}
