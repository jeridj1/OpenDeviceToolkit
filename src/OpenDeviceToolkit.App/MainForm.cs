using OpenDeviceToolkit.Android;
using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.App;

public sealed class MainForm : Form
{
    private readonly Workspace _workspace = new();
    private readonly ILogger _logger;
    private readonly ToolLocator _toolLocator;
    private readonly AndroidReportWriter _reportWriter = new();
    private readonly Label _status = new();
    private readonly Label _deviceSummary = new();
    private readonly TextBox _output = new();
    private readonly Button _scanButton = new();
    private readonly Button _environmentButton = new();
    private AndroidDevice? _device;
    private AdbManager? _adb;

    public MainForm()
    {
        _workspace.EnsureDirectories();
        _logger = new FileLogger(_workspace);
        _toolLocator = new ToolLocator(_workspace);
        _logger.Info("Application started.");

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

        _environmentButton.Text = "Environment Check";
        _environmentButton.AutoSize = true;
        _environmentButton.Location = new Point(430, 95);
        _environmentButton.Click += (_, _) => ShowEnvironmentDiagnostics();

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
        _status.Text = "Status\r\n------\r\nReady.\r\n\r\nWorkspace:\r\n" + _workspace.Root;
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        Controls.AddRange([title, subtitle, _scanButton, reportButton, workspaceButton, _environmentButton, _deviceSummary, _status, _output]);
        Shown += async (_, _) => await ScanAsync();
    }

    private async Task ScanAsync()
    {
        _scanButton.Enabled = false;
        _status.Text = "Status\r\n------\r\nLocating ADB...";
        _logger.Info("Starting device scan.");

        try
        {
            _workspace.EnsureDirectories();
            var adbTool = _toolLocator.Find("adb");
            _logger.Info($"ADB discovery: {(adbTool.Found ? adbTool.Path : "not found")}");

            if (!adbTool.Found || adbTool.Path is null)
            {
                _adb = null;
                _device = null;
                _deviceSummary.Text = "ADB was not found.\r\n\r\nPlace adb.exe in the ODT Tools directory, its platform-tools subdirectory, or on PATH.";
                _status.Text = "Status\r\n------\r\nADB unavailable.\r\nNo changes were made to the phone.";
                return;
            }

            _adb = new AdbManager(new CommandRunner(), adbTool.Path);
            if (!await _adb.IsAvailableAsync())
            {
                _device = null;
                _deviceSummary.Text = "ADB was found but did not respond successfully.";
                _status.Text = "Status\r\n------\r\nADB execution failed.\r\nSee Logs\\odt.log for details.";
                _logger.Warning("ADB executable was found but `adb version` failed.");
                return;
            }

            var devices = await _adb.GetDevicesAsync();
            if (devices.Count == 0)
            {
                _device = null;
                _deviceSummary.Text = "No Android device detected.\r\n\r\nConnect the phone with USB debugging enabled.";
                _status.Text = "Status\r\n------\r\nADB is working.\r\nNo device connected.";
                _logger.Info("ADB returned no devices.");
                return;
            }

            var first = devices[0];
            if (first.State != DeviceConnectionState.Connected)
            {
                _device = null;
                _deviceSummary.Text = $"Device: {first.Serial}\r\nState: {first.State}";
                _status.Text = "Status\r\n------\r\nDevice detected, but it is not authorized/online.";
                _logger.Warning($"Device {first.Serial} state is {first.State}.");
                return;
            }

            _device = await _adb.InspectAsync(first.Serial);
            if (_device is null)
            {
                _deviceSummary.Text = "Device was detected, but inspection failed.";
                _logger.Warning($"Inspection failed for device {first.Serial}.");
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
            _status.Text = "Status\r\n------\r\nADB: Connected\r\nInspection: Complete\r\nMode: Read-only\r\n\r\n" + _workspace.Root;
            _logger.Info($"Inspection complete for {first.Serial}; collected {_device.Properties.Count} properties.");
        }
        catch (Exception ex)
        {
            _logger.Error("Device scan failed.", ex);
            _status.Text = $"Status\r\n------\r\nError: {ex.Message}";
        }
        finally
        {
            _scanButton.Enabled = true;
        }
    }

    private void ShowEnvironmentDiagnostics()
    {
        try
        {
            _workspace.EnsureDirectories();
            var adb = _toolLocator.Find("adb");
            var fastboot = _toolLocator.Find("fastboot");
            var diagnostics = EnvironmentDiagnostics.Collect(_workspace, adb, fastboot);
            var versions = new[] { ToolVersionProbe.Probe(adb), ToolVersionProbe.Probe(fastboot) };

            var lines = diagnostics.Select(d => $"[{(d.Healthy ? "OK" : "CHECK")}] {d.Name}: {d.Value}").ToList();
            lines.Add(string.Empty);
            lines.Add("Tool versions:");
            lines.AddRange(versions.Select(v => v.Successful
                ? $"[OK] {v.Name}: {v.Version}"
                : $"[CHECK] {v.Name}: {v.Error ?? "Version unavailable"}"));

            _output.Text = string.Join(Environment.NewLine, lines);
            _status.Text = "Status\r\n------\r\nEnvironment diagnostics complete.\r\nTool detection and version probing complete.\r\nSee the output panel for evidence.";
            _logger.Info("Environment diagnostics and tool version probing completed.");
        }
        catch (Exception ex)
        {
            _logger.Error("Environment diagnostics failed.", ex);
            MessageBox.Show(this, ex.Message, "Environment check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            _logger.Info($"Report written: {path}");
            MessageBox.Show(this, $"Report saved to:\r\n{path}", "Report created", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.Error("Could not create report.", ex);
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
            _logger.Error("Could not open workspace.", ex);
            MessageBox.Show(this, ex.Message, "Could not open workspace", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
