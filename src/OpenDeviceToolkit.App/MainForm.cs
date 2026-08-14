using OpenDeviceToolkit.Android;
using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Core.Research;
using OpenDeviceToolkit.Core.Speech;
using OpenDeviceToolkit.Hardware.Rp2040;
using System.Runtime.InteropServices;

namespace OpenDeviceToolkit.App;

public sealed class MainForm : Form
{
    private readonly CommandRunner _runner = new();
    private readonly Workspace _workspace;
    private readonly ToolLocator _tools;
    private readonly UsbDeviceEnumerator _usbEnumerator = new();
    private AdbManager _adb;
    private readonly EnvironmentDiagnostics _environmentDiagnostics;
    private readonly AppLogger _logger;
    private readonly AndroidReportWriter _reportWriter = new();
    private readonly AndroidDiagnosticReportWriter _diagnosticReportWriter = new();
    private readonly AndroidCapabilityPlanner _capabilityPlanner = new();
    private readonly ResearchEngine _researchEngine;
    private readonly SpeechService _speechService;
    private readonly IRp2040Controller _rp2040Controller;
    
    private readonly Label _status = new();
    private readonly Label _deviceSummary = new();
    private readonly TextBox _output = new();
    private readonly Button _scanButton = new();
    private readonly Button _environmentButton = new();
    private readonly Button _deepScanButton = new();
    private readonly Button _rebootButton = new();
    private readonly Button _bootloaderButton = new();
    private readonly Button _recoveryButton = new();
    private readonly Button _usbScanButton = new();
    private readonly Button _voiceToggleButton = new();
    private readonly Button _researchButton = new();
    private readonly Button _rp2040Button = new();
    private readonly CheckBox _eWasteModeCheckBox = new();
    private readonly Panel _voicePanel = new();
    private readonly TextBox _voiceInput = new();
    private readonly Button _voiceSendButton = new();
    
    private AndroidDevice? _device;
    private bool _eWasteMode = false;
    
    public MainForm()
    {
        _workspace = Config.Current.Workspace.ToWorkspace();
        _tools = new ToolLocator(_workspace, Config.Current.Adb);
        _adb = new AdbManager(_runner);
        _environmentDiagnostics = new EnvironmentDiagnostics(_workspace, _runner, _usbEnumerator);
        _logger = new AppLogger(_workspace);
        _researchEngine = new ResearchEngine(_workspace, _logger, _runner);
        _speechService = new SpeechService(_logger);
        _rp2040Controller = Rp2040ControllerFactory.GetController();
        
        _speechService.IsEnabled = Config.Current.Voice.Enabled;
        _speechService.Rate = Config.Current.Voice.Rate;
        _speechService.Volume = Config.Current.Voice.Volume;
        _speechService.TextRecognized += OnTextRecognized;
        _speechService.ListeningStarted += () => UpdateVoiceStatus();
        _speechService.ListeningStopped += () => UpdateVoiceStatus();

        _logger.Info("Open Device Toolkit starting...");
        _logger.Info($"Workspace: {_workspace.Root}");
        _logger.Info($"OS: {RuntimeInformation.OSDescription}");
        _logger.Info($".NET: {Environment.Version}");

        Text = "Open Device Toolkit 0.1 Alpha";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1200, 800);
        Font = new Font("Segoe UI", 10F);
        
        var title = new Label { Text = "Open Device Toolkit", Font = new Font("Segoe UI", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(24, 18) };
        var subtitle = new Label { Text = "Device reconnaissance + controlled operations + research engine", AutoSize = true, Location = new Point(27, 58) };

        _scanButton.Text = "Detect Device"; _scanButton.AutoSize = true; _scanButton.Location = new Point(24, 95); _scanButton.Click += async (_, _) => await ScanAsync();
        var reportButton = new Button { Text = "Generate Report", AutoSize = true, Location = new Point(150, 95) }; reportButton.Click += (_, _) => GenerateReport();
        var workspaceButton = new Button { Text = "Open Workspace", AutoSize = true, Location = new Point(295, 95) }; workspaceButton.Click += (_, _) => OpenWorkspace();
        _environmentButton.Text = "Environment Check"; _environmentButton.AutoSize = true; _environmentButton.Location = new Point(440, 95); _environmentButton.Click += async (_, _) => await RunEnvironmentCheckAsync();
        _deepScanButton.Text = "Deep Read-Only Scan"; _deepScanButton.AutoSize = true; _deepScanButton.Location = new Point(590, 95); _deepScanButton.Enabled = false; _deepScanButton.Click += async (_, _) => await RunDeepScanAsync();
        _usbScanButton.Text = "USB Devices"; _usbScanButton.AutoSize = true; _usbScanButton.Location = new Point(740, 95); _usbScanButton.Click += (_, _) => ShowUsbDevices();

        _voiceToggleButton.Text = "Voice (OFF)"; _voiceToggleButton.AutoSize = true; _voiceToggleButton.Location = new Point(24, 130); _voiceToggleButton.Click += (_, _) => ToggleVoiceMode();
        _researchButton.Text = "Research Device"; _researchButton.AutoSize = true; _researchButton.Location = new Point(150, 130); _researchButton.Click += async (_, _) => await StartResearchAsync();
        _rp2040Button.Text = "RP2040 Bridge"; _rp2040Button.AutoSize = true; _rp2040Button.Location = new Point(295, 130); _rp2040Button.Click += async (_, _) => await ShowRp2040DialogAsync();
        _eWasteModeCheckBox.Text = "E-Waste Mode"; _eWasteModeCheckBox.AutoSize = true; _eWasteModeCheckBox.Location = new Point(440, 132); _eWasteModeCheckBox.ToolTipText = "Enable to allow irreversible experiments"; _eWasteModeCheckBox.CheckedChanged += (_, _) => { _eWasteMode = _eWasteModeCheckBox.Checked; UpdateEwasteMode(); };

        _rebootButton.Text = "Reboot"; _rebootButton.AutoSize = true; _rebootButton.Location = new Point(24, 335); _rebootButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootSystem);
        _bootloaderButton.Text = "Reboot Bootloader"; _bootloaderButton.AutoSize = true; _bootloaderButton.Location = new Point(110, 335); _bootloaderButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootBootloader);
        _recoveryButton.Text = "Reboot Recovery"; _recoveryButton.AutoSize = true; _recoveryButton.Location = new Point(270, 335); _recoveryButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootRecovery);

        _deviceSummary.BorderStyle = BorderStyle.FixedSingle; _deviceSummary.AutoSize = false; _deviceSummary.Location = new Point(24, 175); _deviceSummary.Size = new Size(440, 150); _deviceSummary.Padding = new Padding(12); _deviceSummary.Text = "No device inspected yet.";

        _output.Multiline = true; _output.ScrollBars = ScrollBars.Both; _output.ReadOnly = true; _output.WordWrap = false; _output.Font = new Font("Consolas", 9.5F); _output.Location = new Point(480, 175); _output.Size = new Size(690, 480); _output.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _status.AutoSize = false; _status.Location = new Point(24, 335); _status.Size = new Size(440, 200); _status.Text = "Status\r\n------\r\nReady.\r\n\r\nWorkspace:\r\n" + _workspace.Root; _status.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        _voicePanel.Location = new Point(24, 640); _voicePanel.Size = new Size(440, 100); _voicePanel.Visible = false; _voicePanel.BorderStyle = BorderStyle.FixedSingle;
        var voiceLabel = new Label { Text = "Voice Input:", AutoSize = true, Location = new Point(10, 10) };
        _voiceInput.Location = new Point(10, 35); _voiceInput.Size = new Size(320, 25); _voiceInput.PlaceholderText = "Speak or type command..."; _voiceInput.KeyPress += (_, e) => { if (e.KeyChar == (char)Keys.Enter) ProcessVoiceInput(); };
        _voiceSendButton.Text = "Send"; _voiceSendButton.AutoSize = true; _voiceSendButton.Location = new Point(335, 35); _voiceSendButton.Click += (_, _) => ProcessVoiceInput();
        _voicePanel.Controls.AddRange(new Control[] { voiceLabel, _voiceInput, _voiceSendButton });

        Controls.AddRange(new Control[] { title, subtitle, _scanButton, reportButton, workspaceButton, _environmentButton, _deepScanButton, _usbScanButton, _voiceToggleButton, _researchButton, _rp2040Button, _eWasteModeCheckBox, _deviceSummary, _status, _output, _voicePanel });

        SetActionButtons(false);
        SetOperationButtons(false);
        Shown += async (_, _) => await ScanAsync();
    }

    // VOICE INTERACTION
    private void ToggleVoiceMode()
    {
        if (_speechService.IsEnabled)
        {
            _speechService.IsEnabled = false;
            _speechService.StopListening();
            _voiceToggleButton.Text = "Voice (OFF)";
            _voicePanel.Visible = false;
            Speak("Voice mode disabled");
        }
        else
        {
            _speechService.IsEnabled = true;
            _speechService.StartListening();
            _voiceToggleButton.Text = "Voice (ON)";
            _voicePanel.Visible = true;
            Speak("Voice mode enabled. Say a command.");
        }
        UpdateVoiceStatus();
    }

    private void OnTextRecognized(string text) => BeginInvoke(() => { _voiceInput.Text = text; ProcessVoiceInput(); });

    private void ProcessVoiceInput()
    {
        var text = _voiceInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _logger.Info($"Voice input: {text}");
        _voiceInput.Clear();
        var command = VoiceCommandParser.Parse(text);
        switch (command.Type)
        {
            case VoiceCommandType.ScanDevice: Speak("Scanning"); _ = ScanAsync(); break;
            case VoiceCommandType.GainAccess: Speak("Researching access"); _ = StartResearchAsync(); break;
            case VoiceCommandType.GenerateReport: Speak("Generating report"); GenerateReport(); break;
            case VoiceCommandType.RebootDevice: Speak("Confirm on screen"); _ = RunOperationAsync(AndroidOperationKind.RebootSystem); break;
            case VoiceCommandType.Help: Speak("Say: scan, gain access, report, reboot, or describe your goal"); break;
            case VoiceCommandType.Exit: Speak("Goodbye"); Close(); break;
            default: if (!string.IsNullOrEmpty(command.Objective)) { Speak($"Researching: {command.Objective}"); _ = StartResearchAsync(command.Objective); } break;
        }
    }

    private void Speak(string text) { if (_speechService.IsEnabled) _speechService.Speak(text); }
    private void UpdateVoiceStatus() { var s = _speechService.IsListening ? "Listening..." : _speechService.IsSpeaking ? "Speaking..." : "Ready"; _voiceToggleButton.Text = _speechService.IsEnabled ? $"Voice ({s})" : "Voice (OFF)"; }
    private void UpdateEwasteMode() { _eWasteModeCheckBox.ForeColor = _eWasteMode ? Color.Red : SystemColors.ControlText; }

    // RESEARCH ENGINE
    private async Task StartResearchAsync(string? objective = null)
    {
        if (_device == null) { Speak("Connect device first"); MessageBox.Show(this, "Please connect and detect a device first.", "No Device", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        SetActionButtons(false); _status.Text = "Status\r\n------\r\nStarting research...\r\n"; _output.Text = "";
        try
        {
            objective ??= $"Gain full access to {_device.Manufacturer} {_device.Model}";
            var session = _researchEngine.StartSession(_device.Serial, objective);
            _status.Text += "Searching for methods...\r\n"; Speak("Searching for access methods");
            var results = await _researchEngine.SearchAsync(objective, CancellationToken.None);
            foreach (var r in results) _output.Text += $"[RESEARCH] {r.Display}\r\n{r.Url}\r\n\r\n";
            var hypotheses = _researchEngine.GenerateHypotheses(results);
            var plan = _researchEngine.CreatePlan(_device.Serial, objective, hypotheses);
            _status.Text += $"\r\nFound {results.Count} results, {hypotheses.Count} hypotheses.\r\n" + (_eWasteMode ? "E-WASTE MODE ACTIVE!" : "Safe mode");
            Speak($"Found {hypotheses.Count} potential methods");
        }
        catch (Exception ex) { _status.Text = $"Research failed: {ex.Message}"; _logger.Error("Research failed", ex); Speak("Research failed"); }
        finally { SetActionButtons(true); }
    }

    // RP2040 BRIDGE
    private async Task ShowRp2040DialogAsync()
    {
        try
        {
            _status.Text = "Connecting to RP2040...\r\n"; Speak("Connecting to RP2040");
            var connected = await _rp2040Controller.ConnectAsync();
            if (!connected) { _status.Text += "RP2040 not found\r\n"; Speak("Not found"); return; }
            _status.Text += "Connected!\r\n"; Speak("Connected");
            var modes = string.Join(", ", _rp2040Controller.AvailableModes);
            _output.Text = $"RP2040 Bridge\r\nConnected: {_rp2040Controller.IsConnected}\r\nMode: {_rp2040Controller.CurrentMode}\r\nAvailable: {modes}\r\n\r\nKnown Chips:\r\n" + string.Join("\r\n", PinoutDatabase.GetChipIdentifiers().Take(20).Select(c => $"  - {c}"));
            Speak($"Ready. {_rp2040Controller.AvailableModes.Count} modes available");
        }
        catch (Exception ex) { _status.Text = $"RP2040 error: {ex.Message}\r\n"; _logger.Error("RP2040 error", ex); Speak("Error"); }
    }

    // EXISTING METHODS
    private async Task ScanAsync()
    {
        SetActionButtons(false); _status.Text = "Checking ADB...";
        try
        {
            _workspace.EnsureDirectories(); var adb = _tools.Find("adb");
            if (!adb.Found || string.IsNullOrWhiteSpace(adb.Path)) { _device = null; _deviceSummary.Text = "ADB not found.\r\nInstall Android Platform Tools or place adb.exe in ODT Tools."; _status.Text = "ADB unavailable."; _logger.Warning("ADB not found"); return; }
            _adb = new AdbManager(_runner, adb.Path);
            if (!await _adb.IsAvailableAsync()) { _device = null; _deviceSummary.Text = "ADB found but failed."; _status.Text = "ADB failed."; _logger.Warning("ADB failed"); return; }
            var devices = await _adb.GetDevicesAsync();
            if (devices.Count == 0) { _device = null; _deviceSummary.Text = "No device detected."; _status.Text = "No device connected."; _logger.Info("No devices"); return; }
            var first = devices[0];
            if (first.State != DeviceConnectionState.Connected) { _device = null; _deviceSummary.Text = $"Device: {first.Serial}\r\nState: {first.State}"; _status.Text = "Device not ready."; _logger.Warning($"Device {first.Serial} in state {first.State}"); return; }
            _device = await _adb.InspectAsync(first.Serial);
            if (_device == null) { _deviceSummary.Text = "Inspection failed."; _logger.Warning("Inspection failed"); return; }
            _deviceSummary.Text = $"{_device.Manufacturer} {_device.Model}\r\nAndroid: {_device.AndroidVersion}\r\nPlatform: {_device.Platform}\r\nSerial: {_device.Serial}";
            _output.Text = string.Join(Environment.NewLine, _device.Properties.OrderBy(x => x.Key).Select(x => $"[{x.Key}]: [{x.Value}]"));
            _status.Text = "ADB: Connected\r\nReady.\r\n" + _workspace.Root; _logger.Info($"Device: {_device.Serial}");
            Speak($"Detected {_device.Manufacturer} {_device.Model}");
        }
        catch (Exception ex) { _status.Text = $"Error: {ex.Message}"; _logger.Error("Scan failed", ex); Speak("Scan failed"); }
        finally { SetActionButtons(true); SetOperationButtons(_device != null); }
    }

    private async Task RunOperationAsync(AndroidOperationKind kind)
    {
        if (_device == null) return;
        var desc = kind switch { AndroidOperationKind.RebootSystem => "reboot", AndroidOperationKind.RebootBootloader => "reboot to bootloader", AndroidOperationKind.RebootRecovery => "reboot to recovery", _ => "do this" };
        if (MessageBox.Show(this, $"This will {desc}. Continue?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        SetActionButtons(false); SetOperationButtons(false);
        try
        {
            var result = await new AndroidOperationService(_adb).RebootAsync(_device, kind, true);
            _status.Text = $"Operation: {kind}\r\nExecuted: {result.Executed}\r\n{result.Message}";
            _logger.Info($"Operation {kind}: {result.Executed}"); Speak("Operation complete");
        }
        catch (Exception ex) { _status.Text = $"Failed: {ex.Message}"; _logger.Error("Operation failed", ex); Speak("Failed"); }
        finally { SetActionButtons(true); SetOperationButtons(_device != null); }
    }

    private async Task RunEnvironmentCheckAsync()
    {
        SetActionButtons(false);
        try { var results = await _environmentDiagnostics.RunAsync(); _output.Text = string.Join(Environment.NewLine, results.Select(FormatDiagnostic)); var f = results.Count(x => x.Status == DiagnosticStatus.Fail); var w = results.Count(x => x.Status == DiagnosticStatus.Warning); _status.Text = $"Environment check complete.\r\nFailures: {f}\r\nWarnings: {w}\r\n{_workspace.Root}"; _logger.Info($"Env check: {f} failures, {w} warnings"); Speak($"Check complete. {f} failures"); }
        catch (Exception ex) { _status.Text = $"Failed: {ex.Message}"; _logger.Error("Env check failed", ex); Speak("Failed"); }
        finally { SetActionButtons(true); SetOperationButtons(_device != null); }
    }

    private async Task RunDeepScanAsync()
    {
        if (_device == null) return;
        SetActionButtons(false);
        try
        {
            _status.Text = "Running deep scan...\r\n"; Speak("Running deep scan");
            var diag = await new AndroidDiagnosticService(_adb).RunReadOnlyAsync(_device.Serial);
            var caps = new AndroidCapabilityAnalyzer().Analyze(_device);
            var plan = _capabilityPlanner.Plan(_device);
            _output.Text = string.Join("\r\n\r\n", caps.Select(FormatCapability)) + "\r\n\r\n" + string.Join("\r\n\r\n", plan.Select(FormatPlan)) + "\r\n\r\n" + string.Join("\r\n\r\n", diag.Select(FormatAndroidDiagnostic));
            var failed = diag.Count(x => !x.Success); var report = _diagnosticReportWriter.Write(_workspace, _device, diag); var ready = plan.Count(x => x.Ready);
            _status.Text = $"Deep scan complete.\r\nFailed: {failed}\r\nCapabilities: {caps.Count}\r\nReady: {ready}\r\n{report}";
            _logger.Info($"Deep scan: {failed} failed, {ready} ready"); Speak("Deep scan complete");
        }
        catch (Exception ex) { _status.Text = $"Failed: {ex.Message}"; _logger.Error("Deep scan failed", ex); Speak("Failed"); }
        finally { SetActionButtons(true); SetOperationButtons(_device != null); }
    }

    private void ShowUsbDevices()
    {
        try
        {
            _status.Text = "Enumerating USB...\r\n"; Speak("Enumerating USB devices");
            var devices = _usbEnumerator.EnumerateDevices(); var android = _usbEnumerator.GetAndroidDevices(devices);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== All USB Devices ===");
            foreach (var d in devices) { sb.AppendLine($"Device: {d.DisplayName}\r\nID: {d.DeviceId}\r\n"); if (!string.IsNullOrEmpty(d.VidPid)) sb.AppendLine($"VID/PID: {d.VidPid}\r\n"); }
            sb.AppendLine("=== Android ===");
            if (android.Count == 0) sb.AppendLine("None"); else foreach (var d in android) sb.AppendLine($"{d.DisplayName}\r\n{d.DeviceId}\r\n");
            _output.Text = sb.ToString();
            _status.Text = $"USB done. Total: {devices.Count}, Android: {android.Count}\r\n{_workspace.Root}";
            _logger.Info($"USB: {devices.Count} devices, {android.Count} Android"); Speak($"Found {devices.Count} USB devices");
        }
        catch (Exception ex) { _status.Text = $"Failed: {ex.Message}"; _logger.Error("USB failed", ex); Speak("Failed"); }
    }

    private void SetActionButtons(bool e) { _scanButton.Enabled = e; _environmentButton.Enabled = e; _deepScanButton.Enabled = e && _device != null; _usbScanButton.Enabled = e; _researchButton.Enabled = e && _device != null; _rp2040Button.Enabled = e; }
    private void SetOperationButtons(bool e) { _rebootButton.Enabled = e; _bootloaderButton.Enabled = e; _recoveryButton.Enabled = e; }
    private static string FormatDiagnostic(DiagnosticItem i) => $"[{i.Status.ToString().ToUpper()}] {i.Name}: {i.Value}" + (string.IsNullOrEmpty(i.Evidence) ? "" : $"\r\n    Evidence: {i.Evidence}");
    private static string FormatCapability(AndroidCapability c) => $"[{c.Status.ToString().ToUpper()}] {c.Name}\r\n    Evidence: {c.Evidence}\r\n    Meaning: {c.Explanation}";
    private static string FormatPlan(PlannedOperation p) => $"[{(p.Ready ? "READY" : "BLOCKED")}] {p.Name} ({p.Risk})\r\n    Reason: {p.Reason}\r\n    Preconditions: {string.Join("; ", p.Preconditions)}";
    private static string FormatAndroidDiagnostic(AndroidDiagnosticResult r) => $"[{(r.Success ? "PASS" : "FAIL")}] {r.Name} ({r.Duration.TotalMilliseconds:F0}ms)\r\n{r.Output}";

    private void GenerateReport()
    {
        if (_device == null) { MessageBox.Show(this, "Detect device first.", "No device", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try { var path = _reportWriter.Write(_workspace, _device); _logger.Info($"Report: {path}"); MessageBox.Show(this, $"Saved to:\r\n{path}", "Report", MessageBoxButtons.OK, MessageBoxIcon.Information); Speak("Report saved"); }
        catch (Exception ex) { _logger.Error("Report failed", ex); MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); Speak("Failed"); }
    }

    private void OpenWorkspace()
    {
        try { _workspace.EnsureDirectories(); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{_workspace.Root}\"", UseShellExecute = true }); }
        catch (Exception ex) { _logger.Error("Workspace failed", ex); MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); Speak("Failed"); }
    }
}
