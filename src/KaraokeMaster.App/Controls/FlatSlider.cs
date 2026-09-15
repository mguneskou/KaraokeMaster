using System.Drawing.Drawing2D;
using KaraokeMaster.App.Theming;

namespace KaraokeMaster.App.Controls;

/// <summary>
/// Owner-drawn horizontal slider replacing TrackBar, whose native track/thumb chrome is OS-drawn
/// and ignores BackColor/ForeColor entirely - a light-gray TrackBar sitting on a dark theme looks
/// broken. Drop-in enough for this app's usage: Minimum/Maximum/Value + a Scroll event fired
/// during drag, same shape as the TrackBar.Scroll handlers it replaces.
/// </summary>
public sealed class FlatSlider : Control
{
    private const int ThumbRadius = 7;

    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private bool _dragging;

    public FlatSlider()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw,
            true);
        Height = 22;
    }

    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            Value = Math.Clamp(_value, _minimum, _maximum);
            Invalidate();
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            Value = Math.Clamp(_value, _minimum, _maximum);
            Invalidate();
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, _maximum);
            if (clamped == _value)
            {
                return;
            }

            _value = clamped;
            Invalidate();
        }
    }

    /// <summary>Fired continuously while the thumb is dragged, matching TrackBar.Scroll's usage.</summary>
    public event EventHandler? Scroll;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = true;
        UpdateValueFromMouseX(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
        {
            UpdateValueFromMouseX(e.X);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    private void UpdateValueFromMouseX(int x)
    {
        var trackWidth = Math.Max(1, Width - (ThumbRadius * 2));
        var relative = Math.Clamp(x - ThumbRadius, 0, trackWidth);
        var ratio = (double)relative / trackWidth;
        var newValue = (int)Math.Round(_minimum + (ratio * (_maximum - _minimum)));

        if (newValue == _value)
        {
            return;
        }

        Value = newValue;
        Scroll?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var trackY = Height / 2;
        var trackWidth = Math.Max(1, Width - (ThumbRadius * 2));
        var ratio = _maximum > _minimum ? (double)(_value - _minimum) / (_maximum - _minimum) : 0;
        var thumbX = ThumbRadius + (int)(ratio * trackWidth);

        using (var trackPen = new Pen(UiColors.Border, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(trackPen, ThumbRadius, trackY, Width - ThumbRadius, trackY);
        }

        if (thumbX > ThumbRadius)
        {
            using var filledPen = new Pen(UiColors.Accent, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(filledPen, ThumbRadius, trackY, thumbX, trackY);
        }

        using var thumbBrush = new SolidBrush(Enabled ? UiColors.Accent : UiColors.TextDisabled);
        g.FillEllipse(thumbBrush, thumbX - ThumbRadius, trackY - ThumbRadius, ThumbRadius * 2, ThumbRadius * 2);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }
}
