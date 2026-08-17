using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Hardware.Rp2040;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OpenDeviceToolkit.App;

public sealed class LogicAnalyzerForm : Form
{
    private readonly IRp2040Controller _rp2040Controller;
    private readonly AppLogger _logger;
    private readonly Panel _graphPanel = new();
    private readonly CheckedListBox _pinListBox = new();
    private readonly NumericUpDown _sampleRateInput = new();
    private readonly NumericUpDown _durationInput = new();
    private readonly Button _captureButton = new();
    private readonly Button _stopButton = new();
    private readonly Label _statusLabel = new();
    private LogicCapture? _currentCapture;
    private CancellationTokenSource? _captureCancellation;
    private readonly List<Color> _pinColors = new();

    public LogicAnalyzerForm(IRp2040Controller rp2040Controller, AppLogger logger)
    {
        _rp2040Controller = rp2040Controller;
        _logger = logger;
        InitializeColors();
        InitializeComponents();
    }

    private void InitializeColors() => _pinColors.AddRange(new[] { Color.Red, Color.Blue, Color.Green, Color.Purple, Color.Orange, Color.DarkCyan, Color.Magenta, Color.Brown, Color.Pink, Color.Olive, Color.Teal, Color.Navy, Color.Maroon, Color.Gray, Color.Lime });

    private void InitializeComponents()
    {
        Text = "OpenDeviceToolkit Logic Analyzer"; Width = 1200; Height = 800; StartPosition = FormStartPosition.CenterParent;
        _graphPanel.Dock = DockStyle.Fill; _graphPanel.Paint += (_, e) => DrawCapture(e.Graphics); Controls.Add(_graphPanel);
        var controls = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70, AutoSize = false };
        _pinListBox.Width = 180; for (var pin = 0; pin < 30; pin++) _pinListBox.Items.Add($"GPIO {pin}", pin < 4); controls.Controls.Add(_pinListBox);
        controls.Controls.Add(new Label { Text = "Sample rate", AutoSize = true, Margin = new Padding(8, 8, 2, 0) });
        _sampleRateInput.Minimum = 1; _sampleRateInput.Maximum = 10_000_000; _sampleRateInput.Value = 100_000; _sampleRateInput.Width = 100; controls.Controls.Add(_sampleRateInput);
        controls.Controls.Add(new Label { Text = "Duration ms", AutoSize = true, Margin = new Padding(8, 8, 2, 0) });
        _durationInput.Minimum = 1; _durationInput.Maximum = 60_000; _durationInput.Value = 1000; _durationInput.Width = 80; controls.Controls.Add(_durationInput);
        _captureButton.Text = "Capture"; _captureButton.Click += async (_, _) => await CaptureAsync(); controls.Controls.Add(_captureButton);
        _stopButton.Text = "Stop"; _stopButton.Enabled = false; _stopButton.Click += (_, _) => _captureCancellation?.Cancel(); controls.Controls.Add(_stopButton);
        _statusLabel.AutoSize = true; controls.Controls.Add(_statusLabel); Controls.Add(controls); _graphPanel.BringToFront();
    }

    private async Task CaptureAsync()
    {
        var pins = _pinListBox.CheckedItems.Cast<string>().Select(s => int.Parse(s.AsSpan(5))).ToList();
        if (pins.Count == 0) { _statusLabel.Text = "Select at least one pin."; return; }
        _captureCancellation?.Dispose(); _captureCancellation = new CancellationTokenSource(); _captureButton.Enabled = false; _stopButton.Enabled = true;
        try
        {
            _statusLabel.Text = "Capturing...";
            _currentCapture = await _rp2040Controller.CaptureLogicAsync(pins, TimeSpan.FromMilliseconds((double)_durationInput.Value), (int)_sampleRateInput.Value, _captureCancellation.Token);
            _statusLabel.Text = $"Captured {_currentCapture.Samples.Count} samples."; _graphPanel.Invalidate();
        }
        catch (OperationCanceledException) { _statusLabel.Text = "Capture cancelled."; }
        catch (Exception ex) { _logger.Error($"Logic capture failed: {ex.Message}", ex); _statusLabel.Text = $"Capture failed: {ex.Message}"; }
        finally { _captureButton.Enabled = true; _stopButton.Enabled = false; }
    }

    private void DrawCapture(Graphics g)
    {
        g.Clear(BackColor); if (_currentCapture == null || _currentCapture.Samples.Count == 0) return;
        var pins = _currentCapture.Pins; var width = Math.Max(1, _graphPanel.ClientSize.Width - 20); var rowHeight = Math.Max(20, _graphPanel.ClientSize.Height / Math.Max(1, pins.Count));
        for (var p = 0; p < pins.Count; p++)
        {
            var y = p * rowHeight + rowHeight / 2; using var pen = new Pen(_pinColors[p % _pinColors.Count], 1);
            g.DrawString($"GPIO {pins[p]}", Font, Brushes.Black, 4, p * rowHeight + 2); var previous = false;
            for (var i = 0; i < _currentCapture.Samples.Count; i++)
            {
                var x = 70 + i * (width - 70) / (float)Math.Max(1, _currentCapture.Samples.Count - 1);
                var state = p < _currentCapture.Samples[i].PinStates.Count && _currentCapture.Samples[i].PinStates[p];
                if (i > 0 && state != previous) g.DrawLine(pen, x, state ? y - rowHeight / 3 : y + rowHeight / 3, x, previous ? y - rowHeight / 3 : y + rowHeight / 3);
                g.DrawLine(pen, i == 0 ? 70 : x, state ? y - rowHeight / 3 : y + rowHeight / 3, x, state ? y - rowHeight / 3 : y + rowHeight / 3); previous = state;
            }
        }
    }

    protected override void Dispose(bool disposing) { if (disposing) _captureCancellation?.Dispose(); base.Dispose(disposing); }
}
