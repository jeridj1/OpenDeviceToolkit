using System.Drawing;
using System.Windows.Forms;
using OpenDeviceToolkit.App.Ui;
using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Core.Ai;

namespace OpenDeviceToolkit.App;

/// <summary>
/// Dark-themed application shell: sidebar navigation, live status cards,
/// and quick access to the full Workbench and AI Assist. This is the new
/// entry point; the original MainForm remains available as the Workbench.
/// </summary>
public sealed class DashboardShell : Form
{
    private readonly Panel _sidebar = new()
    {
        Dock = DockStyle.Left,
        Width = 232,
        BackColor = Theme.SidebarBack
    };
    private readonly Panel _content = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.WindowBack,
        AutoScroll = true
    };
    private readonly List<Button> _navButtons = new();
    private readonly AiRouter _router;

    public DashboardShell()
    {
        _router = AiRouter.FromConfig(Config.Current);

        Text = "Open Device Toolkit";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1280, 780);
        Font = Theme.DefaultFont();
        BackColor = Theme.WindowBack;
        ForeColor = Theme.TextPrimary;

        BuildSidebar();
        Controls.Add(_content);
        Controls.Add(_sidebar);
        ShowHome();
    }

    private void BuildSidebar()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(14, 18, 14, 14)
        };
        flow.Controls.Add(new Label
        {
            Text = "OPEN DEVICE\nTOOLKIT",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Size = new Size(200, 48),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 0, 2)
        });
        flow.Controls.Add(new Label
        {
            Text = "Hardware rescue workbench",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 28)
        });
        flow.Controls.Add(CreateNavButton("Dashboard", () => ShowHome()));
        flow.Controls.Add(CreateNavButton("AI Assist", () => ShowAiAssist()));
        flow.Controls.Add(CreateNavButton("Workbench", () => OpenWorkbench()));
        flow.Controls.Add(new Label
        {
            Text = "Read-only by default. Every state-changing operation passes through OperationGuard.",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = Theme.TextSecondary,
            AutoSize = false,
            Size = new Size(200, 56),
            Margin = new Padding(0, 40, 0, 0)
        });
        _sidebar.Controls.Add(flow);
    }

    private Button CreateNavButton(string text, Action onClick)
    {
        var button = new Button
        {
            Name = text,
            Text = "  " + text,
            Tag = "nav",
            Width = 204,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 0, 2)
        };
        button.Click += (sender, e) => onClick();
        _navButtons.Add(button);
        return button;
    }

    private void SetActiveNav(string name)
    {
        foreach (var button in _navButtons)
        {
            var active = button.Name == name;
            button.BackColor = active ? Theme.CardBack : Theme.SidebarBack;
            button.ForeColor = active ? Theme.TextPrimary : Theme.TextSecondary;
        }
    }

    private void ShowHome()
    {
        SetActiveNav("Dashboard");
        _content.Controls.Clear();
        _content.Controls.Add(BuildHome());
    }

    private void ShowAiAssist()
    {
        SetActiveNav("AI Assist");
        _content.Controls.Clear();
        _content.Controls.Add(new AiAssistPanel());
    }

    private void OpenWorkbench()
    {
        SetActiveNav(string.Empty);
        var workbench = new MainForm();
        workbench.Show(this);
    }

    private Control BuildHome()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(16)
        };
        flow.Controls.Add(MakeLabel("Dashboard", 15f, FontStyle.Bold, Theme.TextPrimary, 0, 4));
        flow.Controls.Add(MakeLabel("Identify, inspect, and rescue hardware - read-only by default.", 9.5f, FontStyle.Regular, Theme.TextSecondary, 0, 18));

        var cards = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true
        };
        cards.Controls.Add(Theme.CreateCard("Workspace", Config.Current.Workspace.RootPath, 300));
        cards.Controls.Add(Theme.CreateCard("AI Provider", DescribeAiProvider(), 300));
        cards.Controls.Add(Theme.CreateCard("Safety", "Read-only by default. OperationGuard requires explicit confirmation before any state-changing operation.", 300));
        flow.Controls.Add(cards);

        flow.Controls.Add(MakeLabel("QUICK ACTIONS", 8.5f, FontStyle.Bold, Theme.TextSecondary, 28, 10));
        var actions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true
        };
        actions.Controls.Add(MakeActionButton("Open Workbench", OpenWorkbench));
        actions.Controls.Add(MakeActionButton("AI Assist", () => ShowAiAssist()));
        flow.Controls.Add(actions);
        return flow;
    }

    private string DescribeAiProvider()
    {
        var first = _router.Providers.FirstOrDefault(p => p.IsConfigured);
        return first is null
            ? "Not configured. Open AI Assist to add an API key (Mistral, OpenAI, OpenRouter) or install Ollama for fully local analysis."
            : "Ready: " + first.DisplayName + ". Falls back automatically if a provider fails.";
    }

    private static Label MakeLabel(string text, float size, FontStyle style, Color color, int top, int bottom)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = color,
            Font = new Font("Segoe UI", size, style),
            Margin = new Padding(0, top, 0, bottom)
        };
    }

    private static Button MakeActionButton(string text, Action onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(170, 38),
            Margin = new Padding(0, 0, 12, 0)
        };
        Theme.StyleButton(button);
        button.Click += (sender, e) => onClick();
        return button;
    }
}
