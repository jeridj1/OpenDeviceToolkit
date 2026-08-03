using OpenDeviceToolkit.Android;
using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.App;

public sealed class MainForm : Form
{
    private readonly CommandRunner _runner = new();
    private readonly Workspace _workspace = new();
    private readonly ToolLocator _tools;
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
    private AndroidDevice? _device;

    public MainForm()
    {
        _tools = new ToolLocator(_workspace);
        _adb = new AdbManager(_runner);
        _environmentDiagnostics = new EnvironmentDiagnostics(_workspace, _runner);
        _logger = new AppLogger(_workspace);
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
        _rebootButton.Text = "Reboot"; _rebootButton.AutoSize = true; _rebootButton.Location = new Point(24, 335); _rebootButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootSystem);
        _bootloaderButton.Text = "Reboot Bootloader"; _bootloaderButton.AutoSize = true; _bootloaderButton.Location = new Point(110, 335); _bootloaderButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootBootloader);
        _recoveryButton.Text = "Reboot Recovery"; _recoveryButton.AutoSize = true; _recoveryButton.Location = new Point(270, 335); _recoveryButton.Click += async (_, _) => await RunOperationAsync(AndroidOperationKind.RebootRecovery);
        _deviceSummary.BorderStyle = BorderStyle.FixedSingle; _deviceSummary.AutoSize = false; _deviceSummary.Location = new Point(24, 145); _deviceSummary.Size = new Size(440, 180); _deviceSummary.Padding = new Padding(12); _deviceSummary.Text = "No device inspected yet.";
        _output.Multiline = true; _output.ScrollBars = ScrollBars.Both; _output.ReadOnly = true; _output.WordWrap = false; _output.Font = new Font("Consolas", 9.5F); _output.Location = new Point(480, 145); _output.Size = new Size(590, 450); _output.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _status.AutoSize = false; _status.Location = new Point(24, 375); _status.Size = new Size(440, 220); _status.Text = "Status\r\n------\r\nReady.\r\n\r\nWorkspace:\r\n" + _workspace.Root; _status.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        Controls.AddRange([title, subtitle, _scanButton, reportButton, workspaceButton, _environmentButton, _deepScanButton, _rebootButton, _bootloaderButton, _recoveryButton, _deviceSummary, _status, _output]);
        SetOperationButtons(false);
        Shown += async (_, _) => await ScanAsync();
    }

    private async Task ScanAsync()
    {
        SetActionButtons(false); _status.Text = "Status\r\n------\r\nChecking ADB...";
        try
        {
            _workspace.EnsureDirectories(); var adb = _tools.Find("adb");
            if (!adb.Found || string.IsNullOrWhiteSpace(adb.Path)) { _device = null; _deviceSummary.Text = "ADB was not found.\r\n\r\nInstall Android Platform Tools or place adb.exe in the ODT Tools folder or on PATH."; _status.Text = "Status\r\n------\r\nADB unavailable.\r\nNo changes were made to the phone."; _logger.Warning("ADB was not found during device scan."); return; }
            _adb = new AdbManager(_runner, adb.Path);
            if (!await _adb.IsAvailableAsync()) { _device = null; _deviceSummary.Text = "ADB was found but could not be started.\r\n\r\nRun Environment Check for details."; _status.Text = "Status\r\n------\r\nADB execution failed.\r\nNo changes were made to the phone."; _logger.Warning($"ADB was found at '{adb.Path}' but failed its version check."); return; }
            var devices = await _adb.GetDevicesAsync();
            if (devices.Count == 0) { _device = null; _deviceSummary.Text = "No Android device detected.\r\n\r\nConnect the phone with USB debugging enabled."; _status.Text = "Status\r\n------\r\nADB is working.\r\nNo device connected."; _logger.Info("ADB is available; no Android devices were detected."); return; }
            var first = devices[0];
            if (first.State != DeviceConnectionState.Connected) { _device = null; _deviceSummary.Text = $"Device: {first.Serial}\r\nState: {first.State}"; _status.Text = "Status\r\n------\r\nDevice detected, but it is not authorized/online."; _logger.Warning($"Detected device '{first.Serial}' in state {first.State}."); return; }
            _device = await _adb.InspectAsync(first.Serial);
            if (_device is null) { _deviceSummary.Text = "Device was detected, but inspection failed."; _logger.Warning($"Inspection failed for device '{first.Serial}'."); return; }
            _deviceSummary.Text = $"{_device.Manufacturer} {_device.Model}\r\nAndroid: {_device.AndroidVersion}\r\nSecurity patch: {_device.SecurityPatch}\r\nPlatform: {_device.Platform}\r\nVerified Boot: {_device.BootState}\r\nFlash locked: {_device.FlashLocked}\r\nSlot: {_device.Slot}\r\nSerial: {_device.Serial}";
            _output.Text = string.Join(Environment.NewLine, _device.Properties.OrderBy(x => x.Key).Select(x => $"[{x.Key}]: [{x.Value}]"));
            _status.Text = "Status\r\n------\r\nADB: Connected\r\nInspection: Complete\r\nMode: Read-only until an operation is explicitly confirmed.\r\n\r\n" + _workspace.Root; _logger.Info($"Inspected Android device '{_device.Serial}' ({_device.Manufacturer} {_device.Model}).");
        }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nError: {ex.Message}"; _logger.Error("Device scan failed.", ex); }
        finally { SetActionButtons(true); SetOperationButtons(_device is not null); }
    }

    private async Task RunOperationAsync(AndroidOperationKind kind)
    {
        if (_device is null) return;
        var description = kind switch { AndroidOperationKind.RebootSystem => "reboot the phone", AndroidOperationKind.RebootBootloader => "reboot the phone into its bootloader", AndroidOperationKind.RebootRecovery => "reboot the phone into recovery", _ => "perform this operation" };
        var answer = MessageBox.Show(this, $"This will {description}.\r\n\r\nThe phone will disconnect temporarily. Continue?", "Confirm device operation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        SetActionButtons(false); SetOperationButtons(false);
        try
        {
            var result = await new AndroidOperationService(_adb).RebootAsync(_device, kind, explicitConfirmation: true);
            _status.Text = $"Status\r\n------\r\nOperation: {kind}\r\nExecuted: {result.Executed}\r\n{result.Message}";
            _logger.Info($"Android operation {kind} on '{_device.Serial}' executed={result.Executed}: {result.Message}");
        }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nOperation failed: {ex.Message}"; _logger.Error($"Android operation {kind} failed.", ex); }
        finally { SetActionButtons(true); SetOperationButtons(_device is not null); }
    }

    private async Task RunEnvironmentCheckAsync()
    {
        SetActionButtons(false);
        try { var results = await _environmentDiagnostics.RunAsync(); _output.Text = string.Join(Environment.NewLine, results.Select(FormatDiagnostic)); var failures = results.Count(x => x.Status == DiagnosticStatus.Fail); var warnings = results.Count(x => x.Status == DiagnosticStatus.Warning); _status.Text = "Status\r\n------\r\nEnvironment check complete.\r\n" + $"Failures: {failures}\r\nWarnings: {warnings}\r\n\r\nWorkspace:\r\n{_workspace.Root}"; _logger.Info($"Environment check completed with {failures} failures and {warnings} warnings."); }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nEnvironment check failed: {ex.Message}"; _logger.Error("Environment check failed.", ex); }
        finally { SetActionButtons(true); SetOperationButtons(_device is not null); }
    }

    private async Task RunDeepScanAsync()
    {
        if (_device is null) return;
        SetActionButtons(false);
        try
        {
            _status.Text = "Status\r\n------\r\nRunning read-only Android probes...";
            var diagnostics = await new AndroidDiagnosticService(_adb).RunReadOnlyAsync(_device.Serial);
            var capabilities = new AndroidCapabilityAnalyzer().Analyze(_device);
            var plan = _capabilityPlanner.Plan(_device);
            _output.Text = string.Join(Environment.NewLine + Environment.NewLine, capabilities.Select(FormatCapability)) + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine + Environment.NewLine, plan.Select(FormatPlan)) + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine + Environment.NewLine, diagnostics.Select(FormatAndroidDiagnostic));
            var failed = diagnostics.Count(x => !x.Success); var reportPath = _diagnosticReportWriter.Write(_workspace, _device, diagnostics); var ready = plan.Count(x => x.Ready); var writesBlocked = plan.Count(x => x.Risk == OperationRisk.PersistentWrite && !x.Ready);
            _status.Text = "Status\r\n------\r\nDeep scan complete.\r\n" + $"Failed probes: {failed}\r\nCapabilities analyzed: {capabilities.Count}\r\nNext steps ready: {ready}\r\nPersistent writes blocked: {writesBlocked}\r\n\r\nReport:\r\n{reportPath}";
            _logger.Info($"Deep read-only scan completed for '{_device.Serial}' with {failed} failed probes, {capabilities.Count} capability findings, and {ready} ready next steps.");
        }
        catch (Exception ex) { _status.Text = $"Status\r\n------\r\nDeep scan failed: {ex.Message}"; _logger.Error("Deep Android scan failed.", ex); }
        finally { SetActionButtons(true); SetOperationButtons(_device is not null); }
    }

    private void SetActionButtons(bool enabled) { _scanButton.Enabled = enabled; _environmentButton.Enabled = enabled; _deepScanButton.Enabled = enabled && _device is not null; }
    private void SetOperationButtons(bool enabled) { _rebootButton.Enabled = enabled; _bootloaderButton.Enabled = enabled; _recoveryButton.Enabled = enabled; }
    private static string FormatDiagnostic(DiagnosticItem item) { var evidence = string.IsNullOrWhiteSpace(item.Evidence) ? string.Empty : $"\r\n    Evidence: {item.Evidence}"; return $"[{item.Status.ToString().ToUpperInvariant()}] {item.Name}: {item.Value}{evidence}"; }
    private static string FormatCapability(AndroidCapability item) => $"[{item.Status.ToString().ToUpperInvariant()}] {item.Name}\r\n    Evidence: {item.Evidence}\r\n    Meaning: {item.Explanation}";
    private static string FormatPlan(PlannedOperation item) => $"[{(item.Ready ? "READY" : "BLOCKED")}] {item.Name} ({item.Risk})\r\n    Reason: {item.Reason}\r\n    Preconditions: {string.Join("; ", item.Preconditions)}";
    private static string FormatAndroidDiagnostic(AndroidDiagnosticResult item) => $"[{(item.Success ? "PASS" : "FAIL")}] {item.Name} ({item.Duration.TotalMilliseconds:F0} ms)\r\n{item.Output}";

    private void GenerateReport()
    {
        if (_device is null) { MessageBox.Show(this, "Detect a connected Android device first.", "No device", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try { var path = _reportWriter.Write(_workspace, _device); _logger.Info($"Generated Android report '{path}'."); MessageBox.Show(this, $"Report saved to:\r\n{path}", "Report created", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { _logger.Error("Could not create Android report.", ex); MessageBox.Show(this, ex.Message, "Could not create report", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void OpenWorkspace()
    {
        try { _workspace.EnsureDirectories(); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{_workspace.Root}\"", UseShellExecute = true }); }
        catch (Exception ex) { _logger.Error("Could not open workspace.", ex); MessageBox.Show(this, ex.Message, "Could not open workspace", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
