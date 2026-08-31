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
    private readonly Label _voiceStatusLabel = new();
    private readonly Panel _clarificationPanel = new();
    private readonly Label _clarificationLabel = new();
    private readonly Button _clarificationOption1 = new();
    private readonly Button _clarificationOption2 = new();
    private readonly Button _clarificationOption3 = new();
    private AndroidDevice? _device;
    private bool _eWasteMode = false;
    private ClarificationRequest? _pendingClarification;

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
        _speechService.CommandDetected += OnCommandDetected;
        _speechService.ClarificationNeeded += OnClarificationNeeded;
        _speechService.ListeningStarted += () => UpdateVoiceStatus();
        _speechService.ListeningStopped += () => UpdateVoiceStatus();
        _speechService.SpeakingStarted += () => UpdateVoiceStatus();
        _speechService.SpeakingCompleted += () => UpdateVoiceStatus();
        _logger.Info("Open Device Toolkit starting...");
        _logger.Info($"Workspace: {_workspace.Root}");
        _logger.Info($"OS: {RuntimeInformation.OSDescription}");
        _logger.Info($".NET: {Environment.Version}");
        Text = "Open Device Toolkit 0.1 Alpha";
        StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(900, 600); Size = new Size(1200, 800); Font = new Font("Segoe UI", 10F);
        var title = new Label { Text = "Open Device Toolkit", Font = new Font("Segoe UI", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(24, 18) };
        var subtitle = new Label { Text = "Device reconnaissance + controlled operations + natural voice", AutoSize = true, Location = new Point(27, 58) };
        _scanButton.Text = "Detect Device"; _scanButton.AutoSize = true; _scanButton.Location = new Point(24, 95); _scanButton.Click += async (_, _) => await ScanAsync();
        var reportButton = new Button { Text = "Generate Report", AutoSize = true, Location = new Point(150, 95) }; reportButton.Click += (_, _) => GenerateReport();
        var workspaceButton = new Button { Text = "Open Workspace", AutoSize = true, Location = new Point(295, 95) }; workspaceButton.Click += (_, _) => OpenWorkspace();
        _environmentButton.Text = "Environment Check"; _environmentButton.AutoSize = true; _environmentButton.Location = new Point(440, 95); _environmentButton.Click += async (_, _) => await RunEnvironmentCheckAsync();
        _deepScanButton.Text = "Deep Read-Only Scan"; _deepScanButton.AutoSize = true; _deepScanButton.Location = new Point(590, 95); _deepScanButton.Enabled = false; _deepScanButton.Click += async (_, _) => await RunDeepScanAsync();
        _usbScanButton.Text = "USB Devices"; _usbScanButton.AutoSize = true; _usbScanButton.Location = new Point(740, 95); _usbScanButton.Click += (_, _) => ShowUsbDevices();
        _voiceToggleButton.Text = "Voice (OFF)"; _voiceToggleButton.AutoSize = true; _voiceToggleButton.Location = new Point(24, 130); _voiceToggleButton.Click += (_, _) => ToggleVoiceMode();
        _researchButton.Text = "Research Device"; _researchButton.AutoSize = true; _researchButton.Location = new Point(150, 130); _researchButton.Click += async (_, _) => await StartResearchAsync();
        _rp2040Button.Text = "RP2040 Bridge"; _rp2040Button.AutoSize = true; _rp2040Button.Location = new Point(295, 130); _rp2040Button.Click += async (_, _) => await ShowRp2040DialogAsync();
        _eWasteModeCheckBox.Text = "E-Waste Mode"; _eWasteModeCheckBox.AutoSize = true; _eWasteModeCheckBox.Location = new Point(440, 132); _eWasteModeCheckBox.CheckedChanged += (_, _) => { _eWasteMode = _eWasteModeCheckBox.Checked; UpdateEwasteMode(); };
        _rebootButton.Text = "Reboot"; _rebootButton.AutoSize = true; _rebootButton.Location = new Point(24, 335); _rebootButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootSystem);
        _bootloaderButton.Text = "Reboot Bootloader"; _bootloaderButton.AutoSize = true; _bootloaderButton.Location = new Point(110, 335); _bootloaderButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootBootloader);
        _recoveryButton.Text = "Reboot Recovery"; _recoveryButton.AutoSize = true; _recoveryButton.Location = new Point(270, 335); _recoveryButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootRecovery);
        _deviceSummary.BorderStyle = BorderStyle.FixedSingle; _deviceSummary.AutoSize = false; _deviceSummary.Location = new Point(24, 175); _deviceSummary.Size = new Size(440, 150); _deviceSummary.Padding = new Padding(12); _deviceSummary.Text = "No device inspected yet.";
        _output.Multiline = true; _output.ScrollBars = ScrollBars.Both; _output.ReadOnly = true; _output.WordWrap = false; _output.Font = new Font("Consolas", 9.5F); _output.Location = new Point(480, 175); _output.Size = new Size(690, 400); _output.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _status.AutoSize = false; _status.Location = new Point(24, 335); _status.Size = new Size(440, 160); _status.Text = "Status\r\n------\r\nReady.\r\n\r\nWorkspace:\r\n" + _workspace.Root; _status.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        _voicePanel.Location = new Point(24, 640); _voicePanel.Size = new Size(440, 100); _voicePanel.Visible = false; _voicePanel.BorderStyle = BorderStyle.FixedSingle;
        var voiceLabel = new Label { Text = "Voice Input:", AutoSize = true, Location = new Point(10, 10) };
        _voiceInput.Location = new Point(10, 35); _voiceInput.Size = new Size(320, 25); _voiceInput.PlaceholderText = "Speak or type command..."; _voiceInput.KeyPress += (_, e) => { if (e.KeyChar == (char)Keys.Enter) ProcessVoiceInput(); };
        _voiceSendButton.Text = "Send"; _voiceSendButton.AutoSize = true; _voiceSendButton.Location = new Point(335, 35); _voiceSendButton.Click += (_, _) => ProcessVoiceInput();
        _voiceStatusLabel.AutoSize = true; _voiceStatusLabel.Location = new Point(10, 65); _voiceStatusLabel.Text = "Ready";
        _voicePanel.Controls.AddRange(new Control[] { voiceLabel, _voiceInput, _voiceSendButton, _voiceStatusLabel });
        _clarificationPanel.Location = new Point(24, 750); _clarificationPanel.Size = new Size(440, 80); _clarificationPanel.Visible = false; _clarificationPanel.BorderStyle = BorderStyle.FixedSingle; _clarificationPanel.BackColor = Color.LightYellow;
        _clarificationLabel.AutoSize = false; _clarificationLabel.Location = new Point(10, 10); _clarificationLabel.Size = new Size(420, 60); _clarificationLabel.Text = "Did you mean:";
        _clarificationOption1.AutoSize = true; _clarificationOption1.Location = new Point(20, 40); _clarificationOption1.Click += (_, _) => HandleClarification(1);
        _clarificationOption2.AutoSize = true; _clarificationOption2.Location = new Point(150, 40); _clarificationOption2.Click += (_, _) => HandleClarification(2);
        _clarificationOption3.AutoSize = true; _clarificationOption3.Location = new Point(280, 40); _clarificationOption3.Click += (_, _) => HandleClarification(3);
        _clarificationPanel.Controls.AddRange(new Control[] { _clarificationLabel, _clarificationOption1, _clarificationOption2, _clarificationOption3 });
        Controls.AddRange(new Control[] { title, subtitle, _scanButton, reportButton, workspaceButton, _environmentButton, _deepScanButton, _usbScanButton, _voiceToggleButton, _researchButton, _rp2040Button, _eWasteModeCheckBox, _rebootButton, _bootloaderButton, _recoveryButton, _deviceSummary, _status, _output, _voicePanel, _clarificationPanel });
        SetActionButtons(false); SetOperationButtons(false); Shown += async (_, _) => await ScanAsync();
    }

    private void ToggleVoiceMode() { if (_speechService.IsEnabled) { _speechService.IsEnabled = false; _speechService.StopListening(); _voiceToggleButton.Text = "Voice (OFF)"; _voicePanel.Visible = false; Speak("Voice mode disabled"); } else { _speechService.IsEnabled = true; _speechService.StartListening(); _voiceToggleButton.Text = "Voice (ON)"; _voicePanel.Visible = true; Speak("Voice mode enabled. Just talk naturally - I'll understand you."); } UpdateVoiceStatus(); }
    private void OnTextRecognized(string text) => BeginInvoke(() => { _voiceInput.Text = text; ProcessVoiceInput(); });
    private void OnCommandDetected(VoiceCommand command) => BeginInvoke(() => ProcessCommand(command));
    private void OnClarificationNeeded(ClarificationRequest request) => BeginInvoke(() => { _pendingClarification = request; _clarificationLabel.Text = request.Message; var lines = request.Message.Split('\n'); for (int i = 0; i < Math.Min(3, lines.Length - 1); i++) { var optionText = lines[i + 1].Trim().TrimStart('1', '2', '3', '.', ' '); var button = i == 0 ? _clarificationOption1 : i == 1 ? _clarificationOption2 : _clarificationOption3; button.Text = optionText; button.Visible = true; } _clarificationPanel.Visible = true; Speak(request.Message); });
    private void HandleClarification(int optionNumber) { if (_pendingClarification == null) return; var selection = ClarificationDialog.ParseClarificationResponse(optionNumber.ToString(), 3); if (selection.HasValue) { _clarificationPanel.Visible = false; Speak("Understood."); } _pendingClarification = null; }
    private void ProcessVoiceInput() { var text = _voiceInput.Text.Trim(); if (string.IsNullOrEmpty(text)) return; _logger.Info($"Voice input: {text}"); _voiceInput.Clear(); ProcessCommand(VoiceIntentDetector.DetectIntent(text, _speechService.Context)); }
    private void ProcessCommand(VoiceCommand command) { _logger.Info($"Detected command: {command.Type}, Device: {command.Device}, Objective: {command.Objective}"); switch (command.Type) { case VoiceCommandType.ScanDevice: Speak("Scanning for devices"); _ = ScanAsync(); break; case VoiceCommandType.GainAccess: Speak("Starting access research"); _ = StartResearchAsync(command.Objective); break; case VoiceCommandType.GenerateReport: Speak("Generating report"); GenerateReport(); break; case VoiceCommandType.RebootDevice: Speak("Please confirm reboot on screen"); _ = RunOperationAsync(AndroidOperationKind.RebootSystem); break; case VoiceCommandType.Help: Speak("You can say things like: scan my device, unlock this phone, generate a report, reboot it, or just describe what you want to do in your own words."); break; case VoiceCommandType.Exit: Speak("Goodbye"); Close(); break; case VoiceCommandType.CustomObjective: case VoiceCommandType.Unknown: if (!string.IsNullOrEmpty(command.Objective)) { Speak($"I'll help you with: {command.Objective}"); _ = StartResearchAsync(command.Objective); } else Speak("I didn't understand that. Try saying 'help' for available commands."); break; } }
    private void Speak(string text) { if (_speechService.IsEnabled) _speechService.Speak(text); }
    private void UpdateVoiceStatus() { var status = _speechService.IsListening ? "Listening..." : _speechService.IsSpeaking ? "Speaking..." : "Ready"; _voiceToggleButton.Text = _speechService.IsEnabled ? $"Voice ({status})" : "Voice (OFF)"; _voiceStatusLabel.Text = status; }
    private void UpdateEwasteMode() => _eWasteModeCheckBox.ForeColor = _eWasteMode ? Color.Red : SystemColors.ControlText;

    private async Task StartResearchAsync(string? customObjective = null)
    {
        if (_device == null) { Speak("Please connect and detect a device first"); MessageBox.Show(this, "Please connect and detect a device first.", "No Device", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        SetActionButtons(false); _status.Text = "Status\r\n------\r\nStarting research...\r\n"; _output.Text = "";
        try
        {
            var objective = customObjective ?? $"Gain full access to {_device.Manufacturer} {_device.Model}";
            _speechService.Context.CurrentDevice = _device.Model; _speechService.Context.CurrentObjective = objective;
            _researchEngine.StartSession(_device.Serial, objective);
            _status.Text += "Searching for exploits and methods...\r\n"; Speak("Searching for access methods");
            var results = await _researchEngine.SearchAsync(objective, null, CancellationToken.None);
            foreach (var result in results) _output.Text += $"[RESEARCH] {result.Display}\r\nURL: {result.Url}\r\n\r\n";
            var hypotheses = _researchEngine.GenerateHypotheses(results); _researchEngine.CreatePlan(_device.Serial, objective, hypotheses);
            _status.Text += $"\r\nFound {results.Count} results, {hypotheses.Count} hypotheses.\r\n"; _status.Text += _eWasteMode ? "\r\nE-WASTE MODE: Irreversible experiments allowed!" : "\r\nSafe mode: Only reversible operations allowed."; Speak($"Research complete. Found {hypotheses.Count} potential methods. Review results and use operation buttons.");
        }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nResearch failed: {ex.Message}"; _logger.Error("Research failed", ex); Speak("Research failed"); }
        finally { SetActionButtons(true); }
    }

    private async Task ShowRp2040DialogAsync()
    {
        try { _status.Text = "Status\r\n------\r\nConnecting to RP2040...\r\n"; Speak("Connecting to RP2040 bridge"); var connected = await _rp2040Controller.ConnectAsync(); if (!connected) { _status.Text += "Failed to connect to RP2040\r\n"; Speak("RP2040 not found"); return; } _status.Text += "RP2040 connected!\r\n"; Speak("RP2040 connected successfully"); var modes = string.Join(", ", _rp2040Controller.AvailableModes); _output.Text = $"RP2040 Bridge\r\n================\r\nConnected: {_rp2040Controller.IsConnected}\r\nCurrent Mode: {_rp2040Controller.CurrentMode}\r\nAvailable Modes: {modes}\r\n\r\n"; foreach (var chip in PinoutDatabase.GetChipIdentifiers().Take(20)) _output.Text += $"  - {chip}\r\n"; Speak($"RP2040 ready. {_rp2040Controller.AvailableModes.Count} modes available."); }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nRP2040 error: {ex.Message}\r\n"; _logger.Error("RP2040 error", ex); Speak("RP2040 error occurred"); }
    }

    private async Task ScanAsync()
    {
        SetActionButtons(false); _status.Text = "Status\r\n------\r\nChecking ADB...";
        try
        {
            _workspace.EnsureDirectories(); var adb = _tools.Find("adb");
            if (!adb.Found || string.IsNullOrWhiteSpace(adb.Path)) { _device = null; _deviceSummary.Text = "ADB was not found.\r\n\r\nInstall Android Platform Tools or place adb.exe in the ODT Tools folder or on PATH."; _status.Text = "Status\r\n------\r\nADB unavailable.\r\nNo changes were made to the phone."; _logger.Warning("ADB was not found during device scan."); return; }
            _adb = new AdbManager(_runner, adb.Path); if (!await _adb.IsAvailableAsync()) { _device = null; _deviceSummary.Text = "ADB was found but could not be started.\r\n\r\nRun Environment Check for details."; _status.Text = "Status\r\n------\r\nADB execution failed.\r\nNo changes were made to the phone."; _logger.Warning($"ADB was found at '{adb.Path}' but failed its version check."); return; }
            var devices = await _adb.GetDevicesAsync(); if (devices.Count == 0) { _device = null; _deviceSummary.Text = "No Android device detected.\r\n\r\nConnect the phone with USB debugging enabled."; _status.Text = "Status\r\n------\r\nADB is working.\r\nNo device connected."; _logger.Info("ADB is available; no Android devices were detected."); return; }
            var first = devices[0]; if (first.State != DeviceConnectionState.Connected) { _device = null; _deviceSummary.Text = $"Device: {first.Serial}\r\nState: {first.State}"; _status.Text = "Status\r\n------\r\nDevice detected, but it is not authorized/online."; _logger.Warning($"Detected device '{first.Serial}' in state {first.State}."); return; }
            _device = await _adb.InspectAsync(first.Serial); if (_device is null) { _deviceSummary.Text = "Device was detected, but inspection failed."; _logger.Warning($"Inspection failed for device '{first.Serial}'."); return; }
            _deviceSummary.Text = $"{_device.Manufacturer} {_device.Model}\r\nAndroid: {_device.AndroidVersion}\r\nSecurity patch: {_device.SecurityPatch}\r\nPlatform: {_device.Platform}\r\nVerified Boot: {_device.BootState}\r\nFlash locked: {_device.FlashLocked}\r\nSlot: {_device.Slot}\r\nSerial: {_device.Serial}";
            _output.Text = string.Join(Environment.NewLine, _device.Properties.OrderBy(x => x.Key).Select(x => $"[{x.Key}]: [{x.Value}]")); _status.Text = "Status\r\n------\r\nADB: Connected\r\nInspection: Complete\r\nMode: Read-only until an operation is explicitly confirmed.\r\n\r\n" + _workspace.Root; _logger.Info($"Inspected Android device '{_device.Serial}' ({_device.Manufacturer} {_device.Model})."); _speechService.Context.CurrentDevice = _device.Model; Speak($"Device detected: {_device.Manufacturer} {_device.Model} running Android {_device.AndroidVersion}");
        }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nError: {ex.Message}"; _logger.Error("Device scan failed.", ex); Speak("Device scan failed"); }
        finally { SetActionButtons(true); SetOperationButtons(_device is not null); }
    }

    private async Task RunOperationAsync(AndroidOperationKind kind)
    {
        if (_device is null) return; var description = kind switch { AndroidOperationKind.RebootSystem => "reboot the phone", AndroidOperationKind.RebootBootloader => "reboot the phone into its bootloader", AndroidOperationKind.RebootRecovery => "reboot the phone into recovery", _ => "perform this operation" }; var answer = MessageBox.Show(this, $"This will {description}.\r\n\r\nThe phone will disconnect temporarily. Continue?", "Confirm device operation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2); if (answer != DialogResult.Yes) return; SetActionButtons(false); SetOperationButtons(false);
        try { var result = await new AndroidOperationService(_adb).RebootAsync(_device, kind, explicitConfirmation: true); _status.Text = $"Status\r\n------\r\nOperation: {kind}\r\nExecuted: {result.Executed}\r\n{result.Message}"; _logger.Info($"Android operation {kind} on '{_device.Serial}' executed={result.Executed}: {result.Message}"); Speak($"Operation {kind} executed successfully"); }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nOperation failed: {ex.Message}"; _logger.Error($"Android operation {kind} failed.", ex); Speak("Operation failed"); }
        finally { SetActionButtons(true); SetOperationButtons(_device is not null); }
    }

    private async Task RunEnvironmentCheckAsync()
    {
        SetActionButtons(false); try { var results = await _environmentDiagnostics.RunAsync(); _output.Text = string.Join(Environment.NewLine, results.Select(FormatDiagnostic)); var failures = results.Count(x => x.Status == DiagnosticStatus.Fail); var warnings = results.Count(x => x.Status == DiagnosticStatus.Warning); _status.Text = "Status\r\n------\r\nEnvironment check complete.\r\n" + $"Failures: {failures}\r\nWarnings: {warnings}\r\n\r\nWorkspace:\r\n{_workspace.Root}"; _logger.Info($"Environment check completed with {failures} failures and {warnings} warnings."); Speak($"Environment check complete. {failures} failures, {warnings} warnings."); } catch (Exception ex) { _status.Text = $"Status\r\n------\r\nEnvironment check failed: {ex.Message}"; _logger.Error("Environment check failed.", ex); Speak("Environment check failed"); } finally { SetActionButtons(true); SetOperationButtons(_device is not null); }
    }

    private async Task RunDeepScanAsync()
    {
        if (_device is null) return; SetActionButtons(false);
        try { _status.Text = "Status\r\n------\r\nRunning read-only Android probes..."; Speak("Running deep read-only scan"); var diagnostics = await new AndroidDiagnosticService(_adb).RunReadOnlyAsync(_device.Serial); var capabilities = new AndroidCapabilityAnalyzer().Analyze(_device); var plan = _capabilityPlanner.Plan(_device); _output.Text = string.Join(Environment.NewLine + Environment.NewLine, capabilities.Select(FormatCapability)) + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine + Environment.NewLine, plan.Select(FormatPlan)) + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine + Environment.NewLine, diagnostics.Select(FormatAndroidDiagnostic)); var failed = diagnostics.Count(x => !x.Success); var reportPath = _diagnosticReportWriter.Write(_workspace, _device, diagnostics); var ready = plan.Count(x => x.Ready); var writesBlocked = plan.Count(x => x.Risk == OperationRisk.PersistentWrite && !x.Ready); _status.Text = "Status\r\n------\r\nDeep scan complete.\r\n" + $"Failed probes: {failed}\r\nCapabilities analyzed: {capabilities.Count}\r\nNext steps ready: {ready}\r\nPersistent writes blocked: {writesBlocked}\r\n\r\nReport:\r\n{reportPath}"; _logger.Info($"Deep read-only scan completed for '{_device.Serial}' with {failed} failed probes, {capabilities.Count} capability findings, and {ready} ready next steps."); Speak($"Deep scan complete. {failed} failed probes, {ready} ready steps."); }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nDeep scan failed: {ex.Message}"; _logger.Error("Deep Android scan failed.", ex); Speak("Deep scan failed"); } finally { SetActionButtons(true); SetOperationButtons(_device is not null); }
    }

    private void ShowUsbDevices()
    {
        try { _status.Text = "Status\r\n------\r\nEnumerating USB devices..."; Speak("Enumerating USB devices"); var devices = _usbEnumerator.EnumerateDevices(); var androidDevices = _usbEnumerator.GetAndroidDevices(devices); var sb = new System.Text.StringBuilder(); sb.AppendLine("=== All USB Devices ==="); foreach (var device in devices) { sb.AppendLine($"Device: {device.DisplayName}"); sb.AppendLine($"  ID: {device.DeviceId}"); if (!string.IsNullOrWhiteSpace(device.VidPid)) sb.AppendLine($"  VID/PID: {device.VidPid}"); sb.AppendLine(); } sb.AppendLine("=== Android Devices ==="); if (androidDevices.Count == 0) sb.AppendLine("No Android devices detected."); else foreach (var device in androidDevices) { sb.AppendLine($"Device: {device.DisplayName}"); sb.AppendLine($"  ID: {device.DeviceId}"); sb.AppendLine(); } _output.Text = sb.ToString(); _status.Text = "Status\r\n------\r\nUSB enumeration complete.\r\n" + $"Total devices: {devices.Count}\r\nAndroid devices: {androidDevices.Count}\r\n\r\nWorkspace:\r\n{_workspace.Root}"; _logger.Info($"USB enumeration completed: {devices.Count} devices, {androidDevices.Count} Android."); Speak($"USB enumeration complete. Found {devices.Count} devices."); }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nUSB enumeration failed: {ex.Message}"; _logger.Error("USB enumeration failed.", ex); Speak("USB enumeration failed"); }
    }

    private void SetActionButtons(bool enabled) { _scanButton.Enabled = enabled; _environmentButton.Enabled = enabled; _deepScanButton.Enabled = enabled && _device is not null; _usbScanButton.Enabled = enabled; _researchButton.Enabled = enabled && _device is not null; _rp2040Button.Enabled = enabled; }
    private void SetOperationButtons(bool enabled) { _rebootButton.Enabled = enabled; _bootloaderButton.Enabled = enabled; _recoveryButton.Enabled = enabled; }
    private static string FormatDiagnostic(DiagnosticItem item) { var evidence = string.IsNullOrWhiteSpace(item.Evidence) ? string.Empty : $"\r\n    Evidence: {item.Evidence}"; return $"[{item.Status.ToString().ToUpperInvariant()}] {item.Name}: {item.Value}{evidence}"; }
    private static string FormatCapability(AndroidCapability item) => $"[{item.Status.ToString().ToUpperInvariant()}] {item.Name}\r\n    Evidence: {item.Evidence}\r\n    Meaning: {item.Explanation}";
    private static string FormatPlan(PlannedOperation item) => $"[{(item.Ready ? "READY" : "BLOCKED")}] {item.Name} ({item.Risk})\r\n    Reason: {item.Reason}\r\n    Preconditions: {string.Join("; ", item.Preconditions)}";
    private static string FormatAndroidDiagnostic(AndroidDiagnosticResult item) => $"[{(item.Success ? "PASS" : "FAIL")}] {item.Name} ({item.Duration.TotalMilliseconds:F0} ms)\r\n{item.Output}";
    private void GenerateReport() { if (_device is null) { MessageBox.Show(this, "Detect a connected Android device first.", "No device", MessageBoxButtons.OK, MessageBoxIcon.Information); Speak("No device detected"); return; } try { var path = _reportWriter.Write(_workspace, _device); _logger.Info($"Generated Android report '{path}'."); MessageBox.Show(this, $"Report saved to:\r\n{path}", "Report created", MessageBoxButtons.OK, MessageBoxIcon.Information); Speak("Report generated and saved"); } catch (Exception ex) { _logger.Error("Could not create Android report.", ex); MessageBox.Show(this, ex.Message, "Could not create report", MessageBoxButtons.OK, MessageBoxIcon.Error); Speak("Failed to generate report"); } }
    private void OpenWorkspace() { try { _workspace.EnsureDirectories(); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{_workspace.Root}\"", UseShellExecute = true }); } catch (Exception ex) { _logger.Error("Could not open workspace.", ex); MessageBox.Show(this, ex.Message, "Could not open workspace", MessageBoxButtons.OK, MessageBoxIcon.Error); Speak("Failed to open workspace"); } }
}
