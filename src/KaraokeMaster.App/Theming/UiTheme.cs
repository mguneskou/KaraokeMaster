using System.Runtime.InteropServices;

namespace KaraokeMaster.App.Theming;

/// <summary>
/// Applies the dark theme to a form and everything inside it. WinForms ambient property
/// inheritance handles plain containers (Panel, FlowLayoutPanel, Label, CheckBox text) for
/// free once the Form's own BackColor/ForeColor is set - this only needs to explicitly style
/// controls with their own opaque, natively-drawn surface (Button, TextBox, ComboBox,
/// DataGridView, ListBox, MenuStrip/StatusStrip, SplitContainer's splitter).
/// </summary>
public static class UiTheme
{
    public static void Apply(Form form)
    {
        form.BackColor = UiColors.WindowBackground;
        form.ForeColor = UiColors.TextPrimary;
        EnableDarkTitleBar(form);

        ApplyRecursive(form);
    }

    private static void ApplyRecursive(Control root)
    {
        foreach (Control control in root.Controls)
        {
            switch (control)
            {
                case Button button:
                    StyleButton(button);
                    break;
                case DataGridView grid:
                    StyleGrid(grid);
                    break;
                case ComboBox combo:
                    StyleComboBox(combo);
                    break;
                case TextBox textBox when textBox.BackColor != Color.Black: // don't reskin SetupWizardForm's terminal-style log box
                    StyleTextBox(textBox);
                    break;
                case ListBox listBox:
                    StyleListBox(listBox);
                    break;
                case SplitContainer split:
                    StyleSplitContainer(split);
                    break;
                case GroupBox groupBox:
                    groupBox.ForeColor = UiColors.TextPrimary;
                    break;
                case MenuStrip menuStrip:
                    StyleToolStrip(menuStrip);
                    break;
                case StatusStrip statusStrip:
                    StyleToolStrip(statusStrip);
                    break;
            }

            ApplyRecursive(control);
        }
    }

    private static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = UiColors.SurfaceRaised;
        button.ForeColor = UiColors.TextPrimary;
        button.FlatAppearance.BorderColor = UiColors.Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = UiColors.SurfaceHover;
        button.FlatAppearance.MouseDownBackColor = UiColors.SurfacePressed;
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = UiColors.SurfaceRaised;
        textBox.ForeColor = UiColors.TextPrimary;
    }

    private static void StyleListBox(ListBox listBox)
    {
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.BackColor = UiColors.Surface;
        listBox.ForeColor = UiColors.TextPrimary;
    }

    private static void StyleComboBox(ComboBox combo)
    {
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = UiColors.SurfaceRaised;
        combo.ForeColor = UiColors.TextPrimary;

        // FlatStyle.Flat alone still lets the OS visual-style theme (uxtheme.dll) paint the
        // closed box's background/border on modern Windows, which shows up as a plain white box
        // regardless of BackColor - stripping the native theme forces fully custom rendering.
        SetWindowTheme(combo.Handle, string.Empty, string.Empty);

        // The closed box respects BackColor/ForeColor once FlatStyle is Flat, but the dropdown
        // popup list is a native Win32 listbox that ignores them entirely without owner-draw.
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.ItemHeight = Math.Max(combo.ItemHeight, 18);
        combo.DrawItem -= ComboBox_DrawItem;
        combo.DrawItem += ComboBox_DrawItem;
    }

    private static void ComboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox combo)
        {
            return;
        }

        var selected = e.State.HasFlag(DrawItemState.Selected);
        var background = selected ? UiColors.SurfaceHover : UiColors.SurfaceRaised;
        using var backgroundBrush = new SolidBrush(background);
        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

        if (e.Index >= 0 && e.Index < combo.Items.Count)
        {
            var text = combo.GetItemText(combo.Items[e.Index]);
            using var textBrush = new SolidBrush(UiColors.TextPrimary);
            e.Graphics.DrawString(text, e.Font ?? combo.Font, textBrush, e.Bounds.X + 2, e.Bounds.Y + 1);
        }
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = UiColors.WindowBackground;
        grid.GridColor = UiColors.Border;
        grid.EnableHeadersVisualStyles = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 30;

        grid.ColumnHeadersDefaultCellStyle.BackColor = UiColors.Surface;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = UiColors.TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = UiColors.Surface;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = UiColors.TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);

        grid.DefaultCellStyle.BackColor = UiColors.WindowBackground;
        grid.DefaultCellStyle.ForeColor = UiColors.TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = UiColors.SurfacePressed;
        grid.DefaultCellStyle.SelectionForeColor = UiColors.Accent;
        grid.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);

        grid.AlternatingRowsDefaultCellStyle.BackColor = UiColors.Surface;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = UiColors.TextPrimary;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = UiColors.SurfacePressed;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = UiColors.Accent;

        grid.RowHeadersDefaultCellStyle.BackColor = UiColors.Surface;
        grid.RowHeadersDefaultCellStyle.ForeColor = UiColors.TextSecondary;

        grid.RowTemplate.Height = 26;
    }

    private static void StyleSplitContainer(SplitContainer split)
    {
        split.BackColor = UiColors.SplitterBackground;
        split.Panel1.BackColor = UiColors.WindowBackground;
        split.Panel2.BackColor = UiColors.WindowBackground;
    }

    private static void StyleToolStrip(ToolStrip strip)
    {
        strip.Renderer = new ToolStripProfessionalRenderer(new DarkToolStripColorTable());
        strip.BackColor = UiColors.Surface;
        strip.ForeColor = UiColors.TextPrimary;

        foreach (ToolStripItem item in strip.Items)
        {
            item.ForeColor = UiColors.TextPrimary;
        }
    }

    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? subAppName, string? subIdList);

    public static void EnableDarkTitleBar(Form form)
    {
        void TryApply()
        {
            try
            {
                var enabled = 1;
                DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
            }
            catch (Exception)
            {
                // Pre-2004 Windows 10 without this DWM attribute - title bar just stays light.
            }
        }

        if (form.IsHandleCreated)
        {
            TryApply();
        }
        else
        {
            form.HandleCreated += (_, _) => TryApply();
        }
    }
}
