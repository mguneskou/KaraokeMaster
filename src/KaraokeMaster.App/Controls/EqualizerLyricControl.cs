using System.Drawing.Drawing2D;
using KaraokeMaster.App.Theming;
using KaraokeMaster.Core.Audio;

namespace KaraokeMaster.App.Controls;

/// <summary>
/// Replaces PerformerForm's old RichTextBox lyric display. Draws the equalizer bars and the
/// current lyric line into the same bitmap, bars first, text on top - true alpha transparency
/// between separate WinForms controls is unreliable (RichTextBox in particular doesn't support
/// a transparent BackColor at all), so compositing both onto one canvas in one Paint call is the
/// reliable way to get "equalizer behind the lyrics, both visible" rather than a transparency hack.
/// </summary>
public sealed class EqualizerLyricControl : Control
{
    private const int BarCount = 40;
    private const float BarRiseFactor = 0.45f; // how fast a bar jumps up toward a louder target
    private const float BarFallPerFrame = 0.05f; // how fast a bar drifts down when target is quieter
    private static readonly StringFormat TextMeasureFormat = StringFormat.GenericTypographic;

    private static readonly Color SungColor = Color.Gold;
    private static readonly Color UnsungColor = Color.White;
    private static readonly Color BarColor = Color.FromArgb(110, UiColors.Accent.R, UiColors.Accent.G, UiColors.Accent.B);

    private readonly AudioEngine _audioEngine;
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly float[] _barHeights = new float[BarCount];

    private string _text = string.Empty;
    private int _sungCharEnd;
    private bool _layoutDirty = true;
    private readonly List<LineLayout> _layoutLines = [];
    private float _lineHeight;

    public EqualizerLyricControl(AudioEngine audioEngine)
    {
        _audioEngine = audioEngine;

        SetStyle(
            ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw,
            true);
        BackColor = Color.Black;

        _animationTimer = new System.Windows.Forms.Timer { Interval = 33 }; // ~30fps
        _animationTimer.Tick += (_, _) => AdvanceAnimation();
        _animationTimer.Start();

        Disposed += (_, _) => _animationTimer.Dispose();
    }

    /// <summary>Sets the current line's full text; clears any previous word highlight.</summary>
    public void SetText(string text)
    {
        _text = text ?? string.Empty;
        _sungCharEnd = 0;
        _layoutDirty = true;
        Invalidate();
    }

    /// <summary>
    /// Characters [0, sungCharEnd) render gold (sung), the rest white (unsung) - same convention
    /// PerformerForm's old ApplyWordHighlight used with RichTextBox.Select/SelectionColor.
    /// </summary>
    public void SetSungCharEnd(int sungCharEnd)
    {
        _sungCharEnd = sungCharEnd;
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _layoutDirty = true;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _layoutDirty = true;
    }

    private void AdvanceAnimation()
    {
        var isPlaying = _audioEngine.State == PlaybackState.Playing;
        var targets = isPlaying ? BuildBarTargets(_audioEngine.GetLatestSpectrum()) : null;

        var anyChanged = false;
        for (var i = 0; i < BarCount; i++)
        {
            var target = targets?[i] ?? 0f;
            var previous = _barHeights[i];

            _barHeights[i] = target > previous
                ? previous + ((target - previous) * BarRiseFactor)
                : Math.Max(0f, previous - BarFallPerFrame);

            if (Math.Abs(_barHeights[i] - previous) > 0.001f)
            {
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            Invalidate();
        }
    }

    /// <summary>
    /// Groups the raw (linear-spaced) FFT bins into BarCount log-spaced bars - music energy is
    /// concentrated in low frequencies, so equal-width linear bins would make only the first two
    /// or three bars ever visibly move. Also log-compresses magnitude for the same reason.
    /// </summary>
    private static float[] BuildBarTargets(float[] spectrum)
    {
        var bars = new float[BarCount];
        if (spectrum.Length < 2)
        {
            return bars;
        }

        const int minBin = 1; // skip DC
        var maxBin = spectrum.Length - 1;

        for (var barIndex = 0; barIndex < BarCount; barIndex++)
        {
            var t0 = (double)barIndex / BarCount;
            var t1 = (double)(barIndex + 1) / BarCount;
            var startBin = minBin + (int)(Math.Pow(t0, 2.0) * (maxBin - minBin));
            var endBin = Math.Max(startBin + 1, minBin + (int)(Math.Pow(t1, 2.0) * (maxBin - minBin)));
            endBin = Math.Min(endBin, maxBin);

            var peak = 0f;
            for (var b = startBin; b <= endBin; b++)
            {
                peak = Math.Max(peak, spectrum[b]);
            }

            var compressed = MathF.Log10(1 + (peak * 40));
            bars[barIndex] = Math.Clamp(compressed / 2.2f, 0f, 1f);
        }

        return bars;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Black);

        DrawBars(g);
        DrawLyricText(g);
    }

    private void DrawBars(Graphics g)
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        const float barGap = 3f;
        var barWidth = (Width - (barGap * (BarCount - 1))) / BarCount;
        if (barWidth <= 0)
        {
            return;
        }

        var maxBarHeight = Height * 0.92f;
        using var barBrush = new SolidBrush(BarColor);

        for (var i = 0; i < BarCount; i++)
        {
            var barHeight = _barHeights[i] * maxBarHeight;
            if (barHeight < 1f)
            {
                continue;
            }

            var x = i * (barWidth + barGap);
            g.FillRectangle(barBrush, x, Height - barHeight, barWidth, barHeight);
        }
    }

    private void DrawLyricText(Graphics g)
    {
        if (string.IsNullOrEmpty(_text))
        {
            return;
        }

        if (_layoutDirty)
        {
            RebuildLayout(g);
        }

        if (_layoutLines.Count == 0)
        {
            return;
        }

        var totalHeight = _layoutLines.Count * _lineHeight;
        var y = (Height - totalHeight) / 2f;

        using var sungBrush = new SolidBrush(SungColor);
        using var unsungBrush = new SolidBrush(UnsungColor);

        foreach (var line in _layoutLines)
        {
            var x = (Width - line.Width) / 2f;

            foreach (var word in line.Words)
            {
                var brush = word.CharStart < _sungCharEnd ? sungBrush : unsungBrush;
                g.DrawString(word.Text, Font, brush, x, y, TextMeasureFormat);
                x += word.Width + line.SpaceWidth;
            }

            y += _lineHeight;
        }
    }

    private void RebuildLayout(Graphics g)
    {
        _layoutLines.Clear();
        _layoutDirty = false;

        if (string.IsNullOrEmpty(_text))
        {
            return;
        }

        _lineHeight = Font.GetHeight(g) * 1.05f;
        var maxWidth = Math.Max(10, Width - 40);
        var spaceWidth = g.MeasureString(" ", Font, PointF.Empty, TextMeasureFormat).Width;

        var words = _text.Split(' ');
        var currentLine = new List<WordLayout>();
        var currentLineWidth = 0f;
        var charCursor = 0;

        foreach (var wordText in words)
        {
            var wordWidth = g.MeasureString(wordText, Font, PointF.Empty, TextMeasureFormat).Width;
            var widthIfAdded = currentLine.Count == 0
                ? wordWidth
                : currentLineWidth + spaceWidth + wordWidth;

            if (currentLine.Count > 0 && widthIfAdded > maxWidth)
            {
                _layoutLines.Add(new LineLayout(currentLine, currentLineWidth, spaceWidth));
                currentLine = [];
                currentLineWidth = 0f;
            }

            if (currentLine.Count > 0)
            {
                currentLineWidth += spaceWidth;
            }

            currentLine.Add(new WordLayout(wordText, charCursor, wordWidth));
            currentLineWidth += wordWidth;
            charCursor += wordText.Length + 1; // +1 for the single-space separator LrcParser joins words with
        }

        if (currentLine.Count > 0)
        {
            _layoutLines.Add(new LineLayout(currentLine, currentLineWidth, spaceWidth));
        }
    }

    private readonly record struct WordLayout(string Text, int CharStart, float Width);

    private readonly record struct LineLayout(List<WordLayout> Words, float Width, float SpaceWidth);
}
