using OpenDeviceToolkit.Android;
using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.App;

public sealed class MainForm : Form
{
    private readonly Workspace _workspace = new();
    private readonly AdbManager _adb;
    private readonly AppLogger _logger;
    private readonly AndroidReportWriter _reportWriter = new();
    private readonly Label _status = new();
    private readonly Label _deviceSummary = new();
    private readonly TextBox _output = new();
    private readonly Button _scanButton = new();
    private AndroidDevice? _device;

    public MainForm()
    {
        _adb = new AdbManager(new CommandRunner(), workspace: _workspace);
        _logger = new AppLogger(_workspace);
        _logger.Info("application.start", "Open Device Toolkit started", new Dictionary<string, object?>
        {
            ["adbCandidate"] = _adb.AdbPath,
            ["workspace"] = _workspace.Root
        });

        Text = "Open Device Toolkit 0.1 Alpha";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 600);
        Size = new Size(1000, 700);
        Font = new Font("Segoe UI", 10F);

        var title = new Label
        {
            Text = "Open Device Toolkit",
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 18)
        };

        var subtitle = new Label
        {
            Text = "Device reconnaissance workbench • read-only alpha",
            AutoSize = true,
            Location = new Point(27, 58)
        };

        _scanButton.Text = "Detect Device";
        _scanButton.AutoSize = true;
        _scanButton.Location = new Point(24, 95);
        _scanButton.Click += async (_, _) => await ScanAsync();

        var reportButton = new Button { Text = "Generate Report", AutoSize = true, Location = new Point(150, 95) };
        reportButton.Click += (_, _) => GenerateReport();

        var workspaceButton = new Button { Text = "Open Workspace", AutoSize = true, Location = new Point(295, 95) };
        workspaceButton.Click += (_, _) => OpenWorkspace();

        _deviceSummary.BorderStyle = BorderStyle.FixedSingle;
        _deviceSummary.AutoSize = false;
        _deviceSummary.Location = new Point(24, 145);
        _deviceSummary.Size = new Size(440, 180);
        _deviceSummary.Padding = new Padding(12);
        _deviceSummary.Text = "No device inspected yet.";

        _output.Multiline = true;
        _output.ScrollBars = ScrollBars.Both;
        _output.ReadOnly = true;
        _output.WordWrap = false;
        _output.Font = new Font("Consolas", 9.5F);
        _output.Location = new Point(480, 145);
        _output.Size = new Size(480, 450);
        _output.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _status.AutoSize = false;
        _status.Location = new Point(24, 345);
        _status.Size = new Size(440, 250);
        _status.Text = "Status\r\n------\r\nReady.\r\n\r\nADB candidate:\r\n" + _adb.AdbPath + "\r\n\r\nWorkspace:\r\n" + _workspace.Root;
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        Controls.AddRange([title, subtitle, _scanButton, reportButton, workspaceButton, _deviceSummary, _status, _output]);
        Shown += async (_, _) => await ScanAsync();
    }

    private async Task ScanAsync()
    {
        _scanButton.Enabled = false;
        _status.Text = "Status\r\n------\r\nChecking ADB...\r\n\r\nCandidate:\r\n" + _adb.AdbPath;
        _logger.Info("scan.start", "Beginning Android device scan", new Dictionary<string, object?>
        {
            ["adbPath"] = _adb.AdbPath
        });

        try
        {
            _workspace.EnsureDirectories();
            var adbAvailable = await _adb.IsAvailableAsync();
            _logger.Info("adb.availability", "ADB availability check completed", new Dictionary<string, object?>
            {
                ["available"] = adbAvailable,
                ["adbPath"] = _adb.AdbPath
            });

            if (!adbAvailable)
            {
                _device = null;
                _deviceSummary.Text = "ADB was not found.\r\n\r\nInstall Android Platform Tools or place adb.exe in the ODT Tools folder or on PATH.";
                _status.Text = "Status\r\n------\r\nADB unavailable.\r\nNo changes were made to the phone.\r\n\r\nCandidate:\r\n" + _adb.AdbPath;
                return;
            }

            var devices = await _adb.GetDevicesAsync();
            _logger.Info("adb.devices", "ADB device enumeration completed", new Dictionary<string, object?>
            {
                ["count"] = devices.Count
            });

            if (devices.Count == 0)
            {
                _device = null;
                _deviceSummary.Text = "No Android device detected.\r\n\r\nConnect the phone with USB debugging enabled.";
                _status.Text = "Status\r\n------\r\nADB is working.\r\nNo device connected.\r\n\r\nADB:\r\n" + _adb.AdbPath;
                return;
            }

            var first = devices[0];
            if (first.State != DeviceConnectionState.Connected)
            {
                _device = null;
                _deviceSummary.Text = $"Device: {first.Serial}\r\nState: {first.State}";
                _status.Text = "Status\r\n------\r\nDevice detected, but it is not authorized/online.\r\n\r\nADB:\r\n" + _adb.AdbPath;
                _logger.Warning("device.not-ready", "First detected device is not ready for inspection", new Dictionary<string, object?>
                {
                    ["state"] = first.State.ToString()
                });
                return;
            }

            _device = await _adb.InspectAsync(first.Serial);
            if (_device is null)
            {
                _deviceSummary.Text = "Device was detected, but inspection failed.";
                _logger.Warning("device.inspect-failed", "ADB detected a device but getprop inspection failed");
                return;
            }

            _deviceSummary.Text = $"{_device.Manufacturer} {_device.Model}\r\n" +
                                  $"Android: {_device.AndroidVersion}\r\n" +
                                  $"Security patch: {_device.SecurityPatch}\r\n" +
                                  $"Platform: {_device.Platform}\r\n" +
                                  $"Verified Boot: {_device.BootState}\r\n" +
                                  $"Flash locked: {_device.FlashLocked}\r\n" +
                                  $"Slot: {_device.Slot}\r\n" +
                                  $"Serial: {_device.Serial}";
            _output.Text = string.Join(Environment.NewLine, _device.Properties.OrderBy(x => x.Key).Select(x => $"[{x.Key}]: [{x.Value}]"));
            _status.Text = "Status\r\n------\r\nADB: Connected\r\nInspection: Complete\r\nMode: Read-only\r\n\r\nADB:\r\n" + _adb.AdbPath + "\r\n\r\nWorkspace:\r\n" + _workspace.Root;
            _logger.Info("device.inspected", "Android device inspection completed", new Dictionary<string, object?>
            {
                ["propertyCount"] = _device.Properties.Count,
                ["model"] = _device.Model,
                ["platform"] = _device.Platform
            });
        }
        catch (Exception ex)
        {
            _status.Text = $"Status\r\n------\r\nError: {ex.Message}";
            _logger.Error("scan.failed", "Android device scan failed", ex);
        }
        finally
        {
            _scanButton.Enabled = true;
        }
    }

    private void GenerateReport()
    {
        if (_device is null)
        {
            MessageBox.Show(this, "Detect a connected Android device first.", "No device", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var path = _reportWriter.Write(_workspace, _device);
            _logger.Info("report.created", "Android inspection report created", new Dictionary<string, object?>
            {
                ["path"] = path
            });
            MessageBox.Show(this, $"Report saved to:\r\n{path}", "Report created", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.Error("report.failed", "Could not create Android inspection report", ex);
            MessageBox.Show(this, ex.Message, "Could not create report", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenWorkspace()
    {
        try
        {
            _workspace.EnsureDirectories();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_workspace.Root}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.Error("workspace.open-failed", "Could not open workspace", ex);
            MessageBox.Show(this, ex.Message, "Could not open workspace", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
