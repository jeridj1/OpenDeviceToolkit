using OpenDeviceToolkit.Android;
using OpenDeviceToolkit.Core;

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
    private AndroidDevice? _device;

    public MainForm()
    {
        // Initialize with configuration support
        _workspace = Config.Current.Workspace.ToWorkspace();
        _tools = new ToolLocator(_workspace, Config.Current.Adb);
        _adb = new AdbManager(_runner);
        _environmentDiagnostics = new EnvironmentDiagnostics(_workspace, _runner, _usbEnumerator);
        _logger = new AppLogger(_workspace);
        
        _logger.Info("Open Device Toolkit starting...");
        _logger.Info($"Workspace: {_workspace.Root}");
        _logger.Info($"OS: {RuntimeInformation.OSDescription}");
        _logger.Info($".NET: {Environment.Version}");

        Text = "Open Device Toolkit 0.1 Alpha";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1100, 700);
        Font = new Font("Segoe UI", 10F);
        
        var title = new Label { Text = "Open Device Toolkit", Font = new Font("Segoe UI", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(24, 18) };
        var subtitle = new Label { Text = "Device reconnaissance + controlled operations", AutoSize = true, Location = new Point(27, 58) };
        
        _scanButton.Text = "Detect Device"; _scanButton.AutoSize = true; _scanButton.Location = new Point(24, 95); _scanButton.Click += async (_, _) => await ScanAsync();
        var reportButton = new Button { Text = "Generate Report", AutoSize = true, Location = new Point(150, 95) }; reportButton.Click += (_, _) => GenerateReport();
        var workspaceButton = new Button { Text = "Open Workspace", AutoSize = true, Location = new Point(295, 95) }; workspaceButton.Click += (_, _) => OpenWorkspace();
        _environmentButton.Text = "Environment Check"; _environmentButton.AutoSize = true; _environmentButton.Location = new Point(440, 95); _environmentButton.Click += async (_, _) => await RunEnvironmentCheckAsync();
        _deepScanButton.Text = "Deep Read-Only Scan"; _deepScanButton.AutoSize = true; _deepScanButton.Location = new Point(590, 95); _deepScanButton.Enabled = false; _deepScanButton.Click += async (_, _) => await RunDeepScanAsync();
        _usbScanButton.Text = "USB Devices"; _usbScanButton.AutoSize = true; _usbScanButton.Location = new Point(740, 95); _usbScanButton.Click += (_, _) => ShowUsbDevices();
        
        _rebootButton.Text = "Reboot"; _rebootButton.AutoSize = true; _rebootButton.Location = new Point(24, 335); _rebootButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootSystem);
        _bootloaderButton.Text = "Reboot Bootloader"; _bootloaderButton.AutoSize = true; _bootloaderButton.Location = new Point(110, 335); _bootloaderButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootBootloader);
        _recoveryButton.Text = "Reboot Recovery"; _recoveryButton.AutoSize = true; _recoveryButton.Location = new Point(270, 335); _recoveryButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootRecovery);
        
        _deviceSummary.BorderStyle = BorderStyle.FixedSingle; _deviceSummary.AutoSize = false; _deviceSummary.Location = new Point(24, 145); _deviceSummary.Size = new Size(440, 180); _deviceSummary.Padding = new Padding(12); _deviceSummary.Text = "No device inspected yet.";
        
        _output.Multiline = true; _output.ScrollBars = ScrollBars.Both; _output.ReadOnly = true; _output.WordWrap = false; _output.Font = new Font("Consolas", 9.5F); _output.Location = new Point(480, 145); _output.Size = new Size(590, 450); _output.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        
        _status.AutoSize = false; _status.Location = new Point(24, 375); _status.Size = new Size(440, 220); _status.Text = "Status\r\n------\r\nReady.\r\n\r\nWorkspace:\r\n" + _workspace.Root; _status.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        
        Controls.AddRange([title, subtitle, _scanButton, reportButton, workspaceButton, _environmentButton, _deepScanButton, _usbScanButton, _rebootButton, _bootloaderButton, _recoveryButton, _deviceSummary, _status, _output]);
        
        SetOperationButtons(false);
        Shown += async (_, _) => await ScanAsync();
    }

    private async Task ScanAsync()
    {
        SetActionButtons(false); _status.Text = "Status\r\n------\r\nChecking ADB...";
        try
        {
            _workspace.EnsureDirectories(); 
            var adb = _tools.Find("adb");
            
            if (!adb.Found || string.IsNullOrWhiteSpace(adb.Path)) 
            {
                _device = null; 
                _deviceSummary.Text = "ADB was not found.\r\n\r\nInstall Android Platform Tools or place adb.exe in the ODT Tools folder or on PATH."; 
                _status.Text = "Status\r\n------\r\nADB unavailable.\r\nNo changes were made to the phone."; 
                _logger.Warning("ADB was not found during device scan."); 
                return; 
            }
            
            _adb = new AdbManager(_runner, adb.Path);
            if (!await _adb.IsAvailableAsync()) 
            {
                _device = null; 
                _deviceSummary.Text = "ADB was found but could not be started.\r\n\r\nRun Environment Check for details."; 
                _status.Text = "Status\r\n------\r\nADB execution failed.\r\nNo changes were made to the phone."; 
                _logger.Warning($"ADB was found at '{adb.Path}' but failed its version check."); 
                return; 
            }
            
            var devices = await _adb.GetDevicesAsync();
            if (devices.Count == 0) 
            {
                _device = null; 
                _deviceSummary.Text = "No Android device detected.\r\n\r\nConnect the phone with USB debugging enabled."; 
                _status.Text = "Status\r\n------\r\nADB is working.\r\nNo device connected."; 
                _logger.Info("ADB is available; no Android devices were detected."); 
                return; 
            }
            
            var first = devices[0];
            if (first.State != DeviceConnectionState.Connected) 
            {
                _device = null; 
                _deviceSummary.Text = $"Device: {first.Serial}\r\nState: {first.State}"; 
                _status.Text = "Status\r\n------\r\nDevice detected, but it is not authorized/online."; 
                _logger.Warning($