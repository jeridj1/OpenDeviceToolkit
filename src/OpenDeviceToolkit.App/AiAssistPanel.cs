using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenDeviceToolkit.App.Ui;
using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Core.Ai;
using OpenDeviceToolkit.Core.Research;

namespace OpenDeviceToolkit.App;

/// <summary>
/// AI Assist panel: provider setup (bring-your-own-key or local Ollama),
/// diagnostic request entry, risk-envelope selection, and strictly
/// validated recommendations rendered as cards. Actions above the
/// operator's envelope are displayed but blocked; approvals are recorded
/// in the workspace audit log.
/// </summary>
public sealed class AiAssistPanel : Panel
{
    private readonly AiKeyStore _keyStore = new();

    private readonly ComboBox _providerCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
    private readonly TextBox _modelBox = new() { Width = 420 };
    private readonly TextBox _urlBox = new() { Width = 420 };
    private readonly TextBox _keyBox = new() { Width = 420, UseSystemPasswordChar = true };
    private readonly Button _saveButton = new() { Text = "Save provider settings", AutoSize = true };
    private readonly TextBox _objectiveBox = new() { Multiline = true, Width = 420, Height = 60, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _evidenceBox = new() { Multiline = true, Width = 420, Height = 110, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox _envelopeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
    private readonly Button _analyzeButton = new() { Text = "Analyze with AI", AutoSize = true, MinimumSize = new Size(180, 38) };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Theme.TextSecondary };
    private readonly Label _providerStatusLabel = new() { AutoSize = true, ForeColor = Theme.TextSecondary };
    private readonly Label _urlLabel = FieldLabel("OLLAMA SERVER URL");
    private readonly Label _keyLabel = FieldLabel("API KEY (DPAPI-ENCRYPTED ON DISK - NEVER IN A CONFIG FILE)");
    private readonly FlowLayoutPanel _results = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(8, 8, 8, 8)
    };

    private AiRouter _router;

    public AiAssistPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.WindowBack;
        Font = Theme.DefaultFont();
        _router = AiRouter.FromConfig(Config.Current, _keyStore);
        BuildUi();
        RefreshProviderUi();
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.TextSecondary,
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        Margin = new Padding(0, 14, 0, 4)
    };

    private void BuildUi()
    {
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8, 8, 8, 12)
        };

        top.Controls.Add(new Label
        {
            Text = "AI Assist",
            AutoSize = true,
            ForeColor = Theme.TextPrimary,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 2)
        });
        top.Controls.Add(new Label
        {
            Text = "Describe the hardware and what you want. The router tries local Ollama first, then your own cloud keys, and every suggestion is risk-classified before you see it.",
            AutoSize = false,
            Size = new Size(760, 32),
            ForeColor = Theme.TextSecondary,
            Margin = new Padding(0, 0, 0, 8)
        });

        top.Controls.Add(FieldLabel("AI PROVIDER"));
        _providerCombo.Items.AddRange(new object[]
        {
            "Ollama (local, no key needed)",
            "Mistral (cloud, API key)",
            "OpenAI (cloud, API key)",
            "OpenRouter (cloud, API key)"
        });
        _providerCombo.SelectedIndex = 0;
        _providerCombo.SelectedIndexChanged += OnProviderChanged;
        top.Controls.Add(_providerCombo);

        top.Controls.Add(FieldLabel("MODEL NAME"));
        top.Controls.Add(_modelBox);

        top.Controls.Add(_urlLabel);
        top.Controls.Add(_urlBox);

        top.Controls.Add(_keyLabel);
        top.Controls.Add(_keyBox);

        top.Controls.Add(_saveButton);
        _saveButton.Click += OnSaveSettings;
        top.Controls.Add(_providerStatusLabel);

        top.Controls.Add(FieldLabel("WHAT DO YOU WANT TO ACCOMPLISH?"));
        _objectiveBox.Text = string.Empty;
        top.Controls.Add(_objectiveBox);

        top.Controls.Add(FieldLabel("DIAGNOSTIC EVIDENCE (chip markings, voltages, pin activity, research notes)"));
        top.Controls.Add(_evidenceBox);

        top.Controls.Add(FieldLabel("RISK ENVELOPE"));
        _envelopeCombo.Items.AddRange(new object[]
        {
            "Read-only (recommended)",
            "Reversible",
            "E-Waste mode (irreversible - double confirmation)"
        });
        _envelopeCombo.SelectedIndex = 0;
        _envelopeCombo.SelectedIndexChanged += OnEnvelopeChanged;
        top.Controls.Add(_envelopeCombo);

        top.Controls.Add(_analyzeButton);
        _analyzeButton.Click += OnAnalyzeClicked;
        top.Controls.Add(_statusLabel);

        Theme.StyleButton(_saveButton);
        Theme.StyleButton(_analyzeButton);
        Theme.StyleComboBox(_providerCombo);
        Theme.StyleComboBox(_envelopeCombo);
        Theme.StyleTextBox(_modelBox);
        Theme.StyleTextBox(_urlBox);
        Theme.StyleTextBox(_keyBox);
        Theme.StyleTextBox(_objectiveBox);
        Theme.StyleTextBox(_evidenceBox);

        Controls.Add(_results);
        Controls.Add(top);

        OnProviderChanged(this, EventArgs.Empty);
    }

    private AiProviderKind SelectedKind() => _providerCombo.SelectedIndex switch
    {
        1 => AiProviderKind.Mistral,
        2 => AiProviderKind.OpenAi,
        3 => AiProviderKind.OpenRouter,
        _ => AiProviderKind.Ollama
    };

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        var isOllama = SelectedKind() == AiProviderKind.Ollama;
        _urlLabel.Visible = isOllama;
        _urlBox.Visible = isOllama;
        _keyLabel.Visible = !isOllama;
        _keyBox.Visible = !isOllama;

        var ai = Config.Current.Ai;
        _modelBox.Text = SelectedKind() switch
        {
            AiProviderKind.Mistral => ai.MistralModel,
            AiProviderKind.OpenAi => ai.OpenAiModel,
            AiProviderKind.OpenRouter => ai.OpenRouterModel,
            _ => ai.OllamaModel
        };
        _urlBox.Text = ai.OllamaUrl;
    }

    private void OnSaveSettings(object? sender, EventArgs e)
    {
        var ai = Config.Current.Ai;
        switch (SelectedKind())
        {
            case AiProviderKind.Ollama:
                ai.OllamaUrl = string.IsNullOrWhiteSpace(_urlBox.Text) ? OllamaProvider.DefaultUrl : _urlBox.Text.Trim();
                ai.OllamaModel = _modelBox.Text.Trim();
                break;
            case AiProviderKind.Mistral:
                ai.MistralModel = _modelBox.Text.Trim();
                break;
            case AiProviderKind.OpenAi:
                ai.OpenAiModel = _modelBox.Text.Trim();
                break;
            case AiProviderKind.OpenRouter:
                ai.OpenRouterModel = _modelBox.Text.Trim();
                break;
        }

        // Move the selected provider to the front of the fallback priority.
        var wanted = SelectedKind().ToString();
        ai.ProviderPriority.RemoveAll(name => string.Equals(name.Trim(), wanted, StringComparison.OrdinalIgnoreCase));
        ai.ProviderPriority.Insert(0, wanted);

        if (SelectedKind() != AiProviderKind.Ollama && !string.IsNullOrWhiteSpace(_keyBox.Text))
        {
            _keyStore.SaveKey(SelectedKind(), _keyBox.Text);
            _keyBox.Clear();
        }

        Config.Current.Save();
        _router = AiRouter.FromConfig(Config.Current, _keyStore);
        RefreshProviderUi();
        _statusLabel.Text = "Provider settings saved.";
        _statusLabel.ForeColor = Theme.Success;
    }

    private void RefreshProviderUi()
    {
        var first = _router.Providers.FirstOrDefault(p => p.IsConfigured);
        if (first is null)
        {
            _providerStatusLabel.Text = "No provider ready yet. Save an API key above or install Ollama (fully local, no key).";
            _providerStatusLabel.ForeColor = Theme.Warning;
        }
        else
        {
            _providerStatusLabel.Text = "Ready: " + first.DisplayName + ". Providers are tried in priority order with automatic fallback.";
            _providerStatusLabel.ForeColor = Theme.Success;
        }
    }

    private RiskLevel SelectedEnvelope() => _envelopeCombo.SelectedIndex switch
    {
        1 => RiskLevel.Reversible,
        2 => RiskLevel.EWasteMode,
        _ => RiskLevel.ReadOnly
    };

    private void OnEnvelopeChanged(object? sender, EventArgs e)
    {
        if (_envelopeCombo.SelectedIndex != 2)
            return;

        var first = MessageBox.Show(
            "E-Waste mode allows the AI to propose IRREVERSIBLE actions that can permanently destroy the device.\n\nUse this only for hardware you own and accept losing. Continue?",
            "Enable E-Waste risk envelope?",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (first != DialogResult.Yes)
        {
            ResetEnvelope();
            return;
        }

        var second = MessageBox.Show(
            "Final confirmation: irreversible actions may permanently brick this device, and everything will be recorded in the audit log. Enable E-Waste mode?",
            "Really enable E-Waste mode?",
            MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
        if (second != DialogResult.Yes)
            ResetEnvelope();
    }

    private void ResetEnvelope()
    {
        _envelopeCombo.SelectedIndexChanged -= OnEnvelopeChanged;
        _envelopeCombo.SelectedIndex = 0;
        _envelopeCombo.SelectedIndexChanged += OnEnvelopeChanged;
    }

    private async void OnAnalyzeClicked(object? sender, EventArgs e)
    {
        if (!_router.AnyConfigured)
        {
            _statusLabel.Text = "No AI provider is ready. Save an API key or install Ollama first.";
            _statusLabel.ForeColor = Theme.Warning;
            return;
        }

        var request = new AiRequest
        {
            Objective = _objectiveBox.Text.Trim(),
            DiagnosticContext = _evidenceBox.Text.Trim(),
            TargetSummary = "Operator-provided hardware (see evidence)",
            AuthorizedEnvelope = SelectedEnvelope()
        };

        _analyzeButton.Enabled = false;
        _statusLabel.Text = "Analyzing with AI (this can take a minute)...";
        _statusLabel.ForeColor = Theme.TextSecondary;
        try
        {
            var result = await _router.AnalyzeAsync(request);
            RenderResult(result);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "AI request failed: " + ex.Message;
            _statusLabel.ForeColor = Theme.Danger;
        }
        finally
        {
            _analyzeButton.Enabled = true;
        }
    }

    private void RenderResult(AiRouterResult result)
    {
        _results.Controls.Clear();

        if (result.Status != AiRouterStatus.Ok || result.Recommendation is null)
        {
            var details = result.ProviderErrors.Count > 0
                ? result.Message + " " + string.Join(" | ", result.ProviderErrors)
                : result.Message;
            _statusLabel.Text = details;
            _statusLabel.ForeColor = Theme.Warning;
            return;
        }

        _statusLabel.Text = result.Message;
        _statusLabel.ForeColor = Theme.Success;

        var recommendation = result.Recommendation;
        _results.Controls.Add(Theme.CreateCard("AI Summary", recommendation.Summary, 780));
        foreach (var warning in recommendation.Warnings)
            _results.Controls.Add(Theme.CreateCard("Warning", warning, 780, Theme.Warning));
        foreach (var action in recommendation.Actions)
            _results.Controls.Add(CreateActionCard(action, recommendation.Provider));
    }

    private Control CreateActionCard(AiProposedAction action, AiProviderKind provider)
    {
        var riskColor = action.Risk switch
        {
            RiskLevel.ReadOnly => Theme.Success,
            RiskLevel.Reversible => Theme.Accent,
            RiskLevel.PersistentWrite => Theme.Warning,
            _ => Theme.Danger
        };

        var body = new StringBuilder();
        body.AppendLine(action.Description);
        body.AppendLine();
        body.Append("Instrument: ").AppendLine(action.Mode);
        if (action.Parameters.Count > 0)
            body.Append("Parameters: ").AppendLine(string.Join(", ", action.Parameters.Select(p => p.Key + "=" + p.Value)));
        if (action.Status != AiActionStatus.Proposed)
            body.AppendLine().AppendLine(action.StatusReason);

        var card = Theme.CreateCard(action.Risk.ToString().ToUpperInvariant() + " ACTION", body.ToString(), 780, riskColor);

        var statusText = action.Status switch
        {
            AiActionStatus.BlockedByEnvelope => "BLOCKED - above the authorized risk envelope",
            AiActionStatus.Malformed => "REJECTED - failed validation",
            _ => action.Risk.GetDescription()
        };
        var statusLabel = new Label
        {
            Text = statusText,
            AutoSize = true,
            ForeColor = riskColor,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Location = new Point(16, card.Height - 32)
        };
        card.Controls.Add(statusLabel);

        if (action.Status == AiActionStatus.Proposed)
        {
            var approveButton = new Button
            {
                Text = "Approve",
                AutoSize = true,
                Location = new Point(card.Width - 110, 10)
            };
            Theme.StyleButton(approveButton);
            approveButton.Click += (sender, e) =>
            {
                LogApproval(action, provider);
                approveButton.Enabled = false;
                approveButton.Text = "Approved + logged";
                statusLabel.Text = "APPROVED - recorded in the workspace audit log. RP2040 execution wiring lands with the controller milestone (see HANDOFF.md).";
            };
            card.Controls.Add(approveButton);
        }

        return card;
    }

    private void LogApproval(AiProposedAction action, AiProviderKind provider)
    {
        try
        {
            var workspace = Config.Current.Workspace.ToWorkspace();
            workspace.EnsureDirectories();
            var directory = Path.Combine(workspace.Root, "Reports");
            Directory.CreateDirectory(directory);

            var line = string.Join(" | ",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "provider=" + provider,
                "risk=" + action.Risk,
                "mode=" + action.Mode,
                "approved",
                action.Description);
            File.AppendAllText(Path.Combine(directory, "ai-assist-audit.log"), line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Could not write the audit log: " + ex.Message;
            _statusLabel.ForeColor = Theme.Warning;
        }
    }
}
