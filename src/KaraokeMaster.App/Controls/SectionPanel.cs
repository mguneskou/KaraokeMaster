using KaraokeMaster.App.Theming;

namespace KaraokeMaster.App.Controls;

/// <summary>
/// Section container replacing GroupBox: GroupBox's border is OS visual-style-drawn and doesn't
/// respect theme colors without full owner-draw, so this is a Panel with a docked title label and
/// a thin accent underline instead - a clean, fully themed section header.
/// </summary>
public sealed class SectionPanel : Panel
{
    private readonly Label _titleLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 30,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(4, 0, 0, 0),
        ForeColor = UiColors.TextSecondary,
    };

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    public SectionPanel()
    {
        _titleLabel.Font = new Font(Font, FontStyle.Bold);
        Controls.Add(_titleLabel);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(UiColors.Border);
        var y = _titleLabel.Bottom;
        e.Graphics.DrawLine(pen, 0, y, Width, y);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Invalidate();
    }
}
