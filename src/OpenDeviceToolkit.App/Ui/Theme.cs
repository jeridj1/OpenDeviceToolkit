using System.Drawing;
using System.Windows.Forms;

namespace OpenDeviceToolkit.App.Ui;

/// <summary>
/// Central dark theme for ODT. All surfaces, text, status colors, and control
/// styling flow through here so the workbench keeps one consistent look and
/// future panels inherit it for free.
/// </summary>
public static class Theme
{
    public static readonly Color WindowBack = Color.FromArgb(15, 17, 21);
    public static readonly Color SidebarBack = Color.FromArgb(20, 23, 28);
    public static readonly Color PanelBack = Color.FromArgb(24, 27, 33);
    public static readonly Color CardBack = Color.FromArgb(31, 35, 43);
    public static readonly Color CardBorder = Color.FromArgb(48, 54, 65);
    public static readonly Color FieldBack = Color.FromArgb(18, 20, 25);
    public static readonly Color TextPrimary = Color.FromArgb(235, 238, 243);
    public static readonly Color TextSecondary = Color.FromArgb(148, 157, 170);
    public static readonly Color Accent = Color.FromArgb(64, 132, 255);
    public static readonly Color AccentHover = Color.FromArgb(88, 150, 255);
    public static readonly Color Success = Color.FromArgb(66, 200, 116);
    public static readonly Color Warning = Color.FromArgb(245, 168, 52);
    public static readonly Color Danger = Color.FromArgb(236, 80, 80);

    private static readonly Font BodyFont = new("Segoe UI", 9.5f);
    private static readonly Font CardTitleFont = new("Segoe UI", 8.5f, FontStyle.Bold);

    public static Font DefaultFont() => BodyFont;

    /// <summary>
    /// Builds a dark card panel with an accent top line. Height is computed
    /// from the wrapped body text so long recommendations stay readable.
    /// </summary>
    public static Panel CreateCard(string title, string body, int width = 300, Color? accent = null)
    {
        var card = new Panel
        {
            BackColor = CardBack,
            Size = new Size(width, 96),
            Margin = new Padding(0, 0, 16, 16)
        };
        var accentColor = accent ?? Accent;
        card.Paint += (sender, e) =>
        {
            using var borderPen = new Pen(CardBorder);
            e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
            using var accentPen = new Pen(accentColor);
            e.Graphics.DrawLine(accentPen, 0, 0, card.Width, 0);
        };

        var titleText = title.ToUpperInvariant();
        var titleSize = TextRenderer.MeasureText(titleText, CardTitleFont);
        var bodySize = TextRenderer.MeasureText(body, BodyFont, new Size(width - 32, int.MaxValue), TextFormatFlags.WordBreak);
        card.Height = Math.Max(96, 24 + titleSize.Height + 6 + bodySize.Height + 14);

        var titleLabel = new Label
        {
            Text = titleText,
            Font = CardTitleFont,
            ForeColor = accentColor,
            AutoSize = true,
            Location = new Point(16, 12)
        };
        var bodyLabel = new Label
        {
            Text = body,
            Font = BodyFont,
            ForeColor = TextPrimary,
            AutoSize = false,
            Size = new Size(width - 32, bodySize.Height + 4),
            Location = new Point(16, 12 + titleSize.Height + 6)
        };
        card.Controls.Add(titleLabel);
        card.Controls.Add(bodyLabel);
        return card;
    }

    public static void ApplyToForm(Form form)
    {
        form.BackColor = WindowBack;
        form.ForeColor = TextPrimary;
        form.Font = DefaultFont();
        StyleControlTree(form.Controls);
    }

    public static void StyleControlTree(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            Style(control);
            if (control.HasChildren)
                StyleControlTree(control.Controls);
        }
    }

    public static void Style(Control control)
    {
        switch (control)
        {
            case Button button:
                StyleButton(button);
                break;
            case TextBox textBox:
                StyleTextBox(textBox);
                break;
            case ComboBox comboBox:
                StyleComboBox(comboBox);
                break;
            case DataGridView grid:
                StyleGrid(grid);
                break;
            case Label label:
                label.ForeColor = label.Tag as string == "secondary" ? TextSecondary : TextPrimary;
                break;
            case GroupBox groupBox:
                groupBox.BackColor = PanelBack;
                groupBox.ForeColor = TextPrimary;
                break;
            case Panel panel:
                panel.BackColor = panel.Tag as string == "card" ? CardBack : PanelBack;
                break;
        }
    }

    public static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.FlatAppearance.MouseOverBackColor = AccentHover;
        button.Cursor = Cursors.Hand;
        button.Height = Math.Max(button.Height, 34);
        button.Padding = new Padding(14, 0, 14, 0);

        // Sidebar navigation buttons opt out of the accent fill via Tag "nav".
        if (button.Tag as string == "nav")
        {
            button.BackColor = SidebarBack;
            button.ForeColor = TextSecondary;
            button.FlatAppearance.MouseOverBackColor = CardBack;
        }
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = FieldBack;
        textBox.ForeColor = TextPrimary;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleComboBox(ComboBox comboBox)
    {
        comboBox.BackColor = FieldBack;
        comboBox.ForeColor = TextPrimary;
        comboBox.FlatStyle = FlatStyle.Flat;
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = PanelBack;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = CardBack;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = CardBack;
        grid.DefaultCellStyle.BackColor = PanelBack;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = Accent;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.GridColor = CardBorder;
    }
}
