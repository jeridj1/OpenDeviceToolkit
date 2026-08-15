using OpenDeviceToolkit.Hardware.Rp2040;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OpenDeviceToolkit.App;

/// <summary>
/// Form for visualizing logic analyzer captures from RP2040.
/// Displays waveform signals for selected GPIO pins.
/// </summary>
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
    
    private void InitializeColors()
    {
        _pinColors.AddRange(new[] {
            Color.Red, Color.Blue, Color.Green, Color.Purple, Color.Orange,
            Color.DarkCyan, Color.Magenta, Color.Brown, Color.Pink, Color.Olive,
            Color.DarkGreen, Color.DarkBlue, Color.DarkRed, Color.Gold, Color.Silver
        });
    }
    
    private void InitializeComponents()
    {
        Text = "RP2040 Logic Analyzer";
        Size = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterParent;
        
        // Left control panel
        var leftPanel = new Panel { 
            Width = 200, 
            Dock = DockStyle.Left, 
            BackColor = SystemColors.ControlLight 
        };
        
        leftPanel.Controls.Add(new Label { 
            Text = "Select Pins to Monitor:", 
            Location = new Point(10, 10), 
            AutoSize = true 
        });
        
        _pinListBox.Location = new Point(10, 30);
        _pinListBox.Size = new Size(180, 200);
        _pinListBox.CheckOnClick = true;
        for (int i = 0; i < 30; i++) _pinListBox.Items.Add($"GP{i}");
        leftPanel.Controls.Add(_pinListBox);
        
        leftPanel.Controls.Add(new Label { 
            Text = "Sample Rate (Hz):", 
            Location = new Point(10, 240), 
            AutoSize = true 
        });
        _sampleRateInput.Location = new Point(10, 260);
        _sampleRateInput.Width = 180;
        _sampleRateInput.Minimum = 1;
        _sampleRateInput.Maximum = 1000000;
        _sampleRateInput.Value = 100000;
        leftPanel.Controls.Add(_sampleRateInput);
        
        leftPanel.Controls.Add(new Label { 
            Text = "Duration (ms):", 
            Location = new Point(10, 290), 
            AutoSize = true 
        });
        _durationInput.Location = new Point(10, 310);
        _durationInput.Width = 180;
        _durationInput.Minimum = 1;
        _durationInput.Maximum = 10000;
        _durationInput.Value = 100;
        leftPanel.Controls.Add(_durationInput);
        
        _captureButton.Text = "Start Capture";
        _captureButton.Location = new Point(10, 340);
        _captureButton.Width = 180;
        _captureButton.Click += async (s, e) => await StartCaptureAsync();
        leftPanel.Controls.Add(_captureButton);
        
        _stopButton.Text = "Stop Capture";
        _stopButton.Location = new Point(10, 370);
        _stopButton.Width = 180;
        _stopButton.Click += CancelCapture;
        _stopButton.Enabled = false;
        leftPanel.Controls.Add(_stopButton);
        
        _statusLabel.Location = new Point(10, 400);
        _statusLabel.AutoSize = true;
        _statusLabel.Text = "Ready";
        leftPanel.Controls.Add(_statusLabel);
        
        Controls.Add(leftPanel);
        
        // Graph display panel
        _graphPanel.Dock = DockStyle.Fill;
        _graphPanel.BackColor = Color.White;
        _graphPanel.Paint += OnGraphPaint;
        Controls.Add(_graphPanel);
        
        Shown += async (s, e) => await InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        try
        {
            if (!_rp2040Controller.IsConnected)
            {
                _statusLabel.Text = "Connecting to RP2040...";
                if (!await _rp2040Controller.ConnectAsync())
                {
                    _statusLabel.Text = "Not connected";
                    MessageBox.Show(this, "Could not connect to RP2040", "Connection Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            
            _statusLabel.Text = "Switching to Logic Analyzer mode...";
            await _rp2040Controller.SwitchModeAsync(Rp2040Mode.LogicAnalyzer);
            _statusLabel.Text = "Ready to capture";
            _logger.Info("Logic Analyzer initialized");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            _logger.Error("Logic analyzer initialization failed", ex);
        }
    }
    
    private async Task StartCaptureAsync()
    {
        var selectedIndices = _pinListBox.CheckedIndices.Cast<int>().ToList();
        if (selectedIndices.Count == 0)
        {
            MessageBox.Show(this, "Select at least one pin", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        var pins = selectedIndices.ToList();
        var sampleRate = (int)_sampleRateInput.Value;
        var duration = TimeSpan.FromMilliseconds((double)_durationInput.Value);
        
        _captureButton.Enabled = false;
        _stopButton.Enabled = true;
        _statusLabel.Text = "Capturing...";
        _graphPanel.Invalidate();
        
        _captureCancellation = new CancellationTokenSource();
        
        try
        {
            _currentCapture = await _rp2040Controller.CaptureLogicAsync(
                pins, duration, sampleRate, _captureCancellation.Token);
            
            _statusLabel.Text = $"Captured {_currentCapture.Samples.Count} samples";
            _logger.Info($"Captured {_currentCapture.Samples.Count} samples from {pins.Count} pins");
            _graphPanel.Invalidate();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            _logger.Error("Capture failed", ex);
        }
        finally
        {
            _captureButton.Enabled = true;
            _stopButton.Enabled = false;
        }
    }
    
    private void CancelCapture(object? sender, EventArgs e)
    {
        _captureCancellation?.Cancel();
        _statusLabel.Text = "Capture cancelled";
        _captureButton.Enabled = true;
        _stopButton.Enabled = false;
        _logger.Info("Capture cancelled by user");
    }
    
    private void OnGraphPaint(object? sender, PaintEventArgs e)
    {
        if (_currentCapture == null) return;
        
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        
        var width = _graphPanel.Width;
        var height = _graphPanel.Height;
        
        // Draw grid
        using var gridPen = new Pen(Color.LightGray);
        for (int x = 0; x < width; x += 50)
            graphics.DrawLine(gridPen, x, 0, x, height);
        for (int y = 0; y < height; y += 50)
            graphics.DrawLine(gridPen, 0, y, width, y);
        
        // Draw waveforms
        var samples = _currentCapture.Samples;
        var pins = _currentCapture.Pins;
        var timeScale = width / (float)_currentCapture.Duration.TotalSeconds;
        var voltageScale = height / (float)(pins.Count + 1);
        
        for (int p = 0; p < pins.Count; p++)
        {
            if (p >= _pinColors.Count) break;
            
            var color = _pinColors[p];
            using var pen = new Pen(color, 2);
            
            var points = new List<PointF>();
            for (int i = 0; i < samples.Count; i++)
            {
                var time = (float)samples[i].Timestamp.TotalSeconds;
                var x = time * timeScale;
                
                var pinIndexInSample = pins.IndexOf(p);
                var isHigh = pinIndexInSample >= 0 && 
                           pinIndexInSample < samples[i].PinStates.Count && 
                           samples[i].PinStates[pinIndexInSample];
                
                var y = height - (p + 0.5f + (isHigh ? 0.3f : -0.3f)) * voltageScale;
                points.Add(new PointF(x, y));
            }
            
            if (points.Count > 1)
                graphics.DrawLines(pen, points.ToArray());
            
            // Draw pin label
            graphics.DrawString($"GP{pins[p]}", Font, Brushes.Black, 
                new PointF(5, height - (p + 0.5f) * voltageScale - 10));
        }
    }
}
