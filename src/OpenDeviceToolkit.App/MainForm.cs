using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Android;
using OpenDeviceToolkit.Hardware.Rp2040;
using System.Drawing;

namespace OpenDeviceToolkit.App;

public sealed class MainForm : Form
{
    private readonly Workspace _workspace;
    private readonly AppLogger _logger;
    private readonly CommandRunner _commandRunner;
    private readonly ToolLocator _toolLocator;
    private readonly ResearchEngine _researchEngine;
    private readonly SpeechService _speechService;
    private AndroidDevice? _device;
    private bool _eWasteMode;
    private readonly Button _scanButton = new();
    private readonly Button _deepScanButton = new();
    private readonly Button _usbScanButton = new();
    private readonly Button _voiceToggleButton = new();
    private readonly Button _researchButton = new();
    private readonly Button _rp2040Button = new();
    private readonly CheckBox _eWasteModeCheckBox = new();
    private readonly Button _rebootButton = new();
    private readonly Button _bootloaderButton = new();
    private readonly Button _recoveryButton = new();
    private readonly Button _environmentButton = new();
    private readonly Button _reportButton = new();
    private readonly Button _workspaceButton = new();
    private readonly Label _deviceSummary = new();
    private readonly Label _status = new();
    private readonly TextBox _output = new();

    public MainForm(Workspace workspace, AppLogger logger, CommandRunner commandRunner, ToolLocator toolLocator, ResearchEngine researchEngine, SpeechService speechService)
    {
        _workspace = workspace; _logger = logger; _commandRunner = commandRunner; _toolLocator = toolLocator; _researchEngine = researchEngine; _speechService = speechService;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "OpenDeviceToolkit"; Width = 1200; Height = 800; StartPosition = FormStartPosition.CenterScreen;
        _scanButton.Text = "Detect Device"; _scanButton.AutoSize = true; _scanButton.Location = new Point(24, 95); _scanButton.Click += async (_, _) => await ScanAsync();
        _reportButton.Text = "Generate Report"; _reportButton.AutoSize = true; _reportButton.Location = new Point(150, 95); _reportButton.Click += (_, _) => GenerateReport();
        _workspaceButton.Text = "Open Workspace"; _workspaceButton.AutoSize = true; _workspaceButton.Location = new Point(295, 95); _workspaceButton.Click += (_, _) => OpenWorkspace();
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
        Controls.AddRange(new Control[] { _scanButton, _reportButton, _workspaceButton, _environmentButton, _deepScanButton, _usbScanButton, _voiceToggleButton, _researchButton, _rp2040Button, _eWasteModeCheckBox, _rebootButton, _bootloaderButton, _recoveryButton, _deviceSummary, _status, _output });
    }

    private async Task ScanAsync() { _status.Text = "Detecting..."; _device = await AndroidDevice.DetectAsync(_commandRunner, _logger); _deepScanButton.Enabled = _device != null; _deviceSummary.Text = _device == null ? "No device detected." : $"{_device.Manufacturer} {_device.Model}\r\nSerial: {_device.Serial}\r\nAndroid: {_device.AndroidVersion}"; _status.Text = _device == null ? "No device detected." : "Device detected."; }
    private void GenerateReport() => _output.AppendText("Report generation is available from the workspace.\r\n");
    private void OpenWorkspace() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _workspace.Root, UseShellExecute = true });
    private async Task RunEnvironmentCheckAsync() { var tools = _toolLocator.FindAllAdbTools(); foreach (var t in tools) _output.AppendText($"{t.Key}: {(t.Value.Found ? t.Value.Path : "not found")}\r\n"); await Task.CompletedTask; }
    private async Task RunDeepScanAsync() { if (_device == null) return; var result = await _device.GetDiagnosticsAsync(CancellationToken.None); foreach (var d in result) _output.AppendText($"{d.Name}: {d.Value}\r\n"); }
    private void ShowUsbDevices() => _output.AppendText("USB enumeration is available through the hardware tools.\r\n");
    private void ToggleVoiceMode() => _output.AppendText("Voice mode toggled.\r\n");
    private void UpdateEwasteMode() => _status.Text = _eWasteMode ? "E-Waste Mode enabled." : "E-Waste Mode disabled.";
    private async Task ShowRp2040DialogAsync() { await Task.CompletedTask; using var controller = new MockRp2040Controller(); using var form = new LogicAnalyzerForm(controller, _logger); form.ShowDialog(this); }
    private async Task RunOperationAsync(AndroidOperationKind operation) { if (_device == null) return; await _device.ExecuteOperationAsync(operation, CancellationToken.None); }
    private async Task StartResearchAsync()
    {
        if (_device == null) { _output.AppendText("Detect a device first.\r\n"); return; }
        var objective = "research device access methods";
        _speechService.Context.CurrentObjective = objective;
        var session = _researchEngine.StartSession(_device.Serial, objective);
        _status.Text += "Searching for exploits and methods...\r\n";
        Speak("Searching for access methods");
        var results = await _researchEngine.SearchAsync(objective, null, CancellationToken.None);
        foreach (var result in results) _output.Text += $"[RESEARCH] {result.Display}\r\nURL: {result.Url}\r\n\r\n";
    }
    private void Speak(string text) { try { _speechService.Speak(text); } catch { } }
}
