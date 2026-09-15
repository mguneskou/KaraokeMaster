using KaraokeMaster.App.Controls;
using KaraokeMaster.App.Theming;
using KaraokeMaster.Core.Audio;
using KaraokeMaster.Core.Lyrics;
using KaraokeMaster.Core.Models;

namespace KaraokeMaster.App.Forms;

/// <summary>
/// The singer-facing display: drag this window (normal bordered mode) onto a second monitor or
/// TV, then press F11 to go borderless-fullscreen on whichever screen it's currently on.
/// </summary>
public sealed class PerformerForm : Form
{
    private readonly AudioEngine _audioEngine;

    private readonly Label _nowSingingLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 60,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font(Control.DefaultFont.FontFamily, 16f, FontStyle.Regular),
        ForeColor = Color.Silver,
    };

    /// <summary>
    /// The "stage": a live audio equalizer filling the whole middle band as a background, with
    /// the current lyric line drawn on top (vertically centered) - see EqualizerLyricControl for
    /// why that's one owner-drawn control instead of a RichTextBox layered over a separate
    /// visualizer control (WinForms control transparency is unreliable, and RichTextBox doesn't
    /// support a transparent background at all).
    /// </summary>
    private readonly EqualizerLyricControl _visualizer;

    private readonly Label _nextLineLabel = new()
    {
        Dock = DockStyle.Bottom,
        Height = 70,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font(Control.DefaultFont.FontFamily, 20f, FontStyle.Regular),
        ForeColor = Color.Gray,
    };

    private readonly Label _hintLabel = new()
    {
        Dock = DockStyle.Bottom,
        Height = 22,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font(Control.DefaultFont.FontFamily, 8f),
        ForeColor = Color.DimGray,
        Text = "Drag to your second display, then press F11 for fullscreen. Esc exits fullscreen.",
    };

    private IReadOnlyList<LrcLine> _lyrics = [];
    private int _activeLineIndex = -1;
    private int _activeWordIndex = -1;
    private int[] _wordCharOffsets = [];

    public PerformerForm(AudioEngine audioEngine)
    {
        _audioEngine = audioEngine;
        _visualizer = new EqualizerLyricControl(_audioEngine)
        {
            Font = new Font(Control.DefaultFont.FontFamily, 40f, FontStyle.Bold),
        };

        Text = "KaraokeMaster — Performer";
        Icon = AppIcon.TryLoad() ?? Icon;
        UiTheme.EnableDarkTitleBar(this);
        BackColor = Color.Black;
        ForeColor = Color.White;
        Width = 900;
        Height = 560;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        Controls.Add(_visualizer);
        Controls.Add(_nextLineLabel);
        Controls.Add(_hintLabel);
        Controls.Add(_nowSingingLabel);

        KeyDown += OnKeyDown;
        Resize += (_, _) => RepositionVisualizer();
        Shown += (_, _) => RepositionVisualizer();

        _audioEngine.PositionChanged += OnPositionChanged;
        FormClosed += (_, _) => _audioEngine.PositionChanged -= OnPositionChanged;

        ShowIdle();
        RepositionVisualizer();
    }

    public void ShowSong(Song song, string? singerName = null)
    {
        _nowSingingLabel.Text = string.IsNullOrWhiteSpace(singerName)
            ? $"Now Singing: {song.Title} — {song.Artist ?? "Unknown Artist"}"
            : $"Now Singing: {singerName} — {song.Title}";
        _activeLineIndex = -1;
        _activeWordIndex = -1;

        _lyrics = song.LrcPath is not null && File.Exists(song.LrcPath)
            ? LrcParser.ParseFile(song.LrcPath)
            : [];

        if (_lyrics.Count == 0)
        {
            SetLineText(song.Title);
            _nextLineLabel.Text = "(no synced lyrics found for this song)";
        }
        else
        {
            SetLineText(string.Empty);
            _nextLineLabel.Text = _lyrics[0].Text;
        }
    }

    private void ShowIdle()
    {
        _nowSingingLabel.Text = "KaraokeMaster";
        SetLineText("Waiting for a song...");
        _nextLineLabel.Text = string.Empty;
    }

    private void OnPositionChanged(object? sender, TimeSpan position)
    {
        if (IsDisposed || _lyrics.Count == 0)
        {
            return;
        }

        var lineIndex = LrcParser.FindActiveIndex(_lyrics, position);
        if (lineIndex != _activeLineIndex)
        {
            BeginInvoke(new Action(() => RenderActiveLine(lineIndex, position)));
            return;
        }

        if (lineIndex < 0)
        {
            return;
        }

        var line = _lyrics[lineIndex];
        if (line.Words.Count == 0)
        {
            return;
        }

        var wordIndex = LrcParser.FindActiveWordIndex(line.Words, position);
        if (wordIndex == _activeWordIndex)
        {
            return;
        }

        BeginInvoke(new Action(() =>
        {
            _activeWordIndex = wordIndex;
            ApplyWordHighlight(line, wordIndex);
        }));
    }

    private void RenderActiveLine(int lineIndex, TimeSpan position)
    {
        _activeLineIndex = lineIndex;

        if (lineIndex < 0)
        {
            SetLineText(string.Empty);
            _nextLineLabel.Text = _lyrics.Count > 0 ? _lyrics[0].Text : string.Empty;
            _wordCharOffsets = [];
            _activeWordIndex = -1;
            return;
        }

        var line = _lyrics[lineIndex];
        SetLineText(line.Text);
        _nextLineLabel.Text = lineIndex + 1 < _lyrics.Count ? _lyrics[lineIndex + 1].Text : string.Empty;

        if (line.Words.Count > 0)
        {
            _wordCharOffsets = ComputeWordCharOffsets(line.Words);
            _activeWordIndex = LrcParser.FindActiveWordIndex(line.Words, position);
            ApplyWordHighlight(line, _activeWordIndex);
        }
        else
        {
            _wordCharOffsets = [];
            _activeWordIndex = -1;
        }
    }

    private void SetLineText(string text) => _visualizer.SetText(text);

    /// <summary>Character offset of each word within the line's joined <see cref="LrcLine.Text"/>.</summary>
    private static int[] ComputeWordCharOffsets(IReadOnlyList<LrcWord> words)
    {
        var offsets = new int[words.Count];
        var cursor = 0;
        for (var i = 0; i < words.Count; i++)
        {
            offsets[i] = cursor;
            cursor += words[i].Text.Length + 1; // +1 for the single space LrcParser joins words with.
        }

        return offsets;
    }

    private void ApplyWordHighlight(LrcLine line, int activeWordIndex)
    {
        if (activeWordIndex < 0 || _wordCharOffsets.Length == 0)
        {
            return;
        }

        var sungEnd = Math.Min(
            _wordCharOffsets[activeWordIndex] + line.Words[activeWordIndex].Text.Length,
            line.Text.Length);

        _visualizer.SetSungCharEnd(sungEnd);
    }

    private void RepositionVisualizer()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        var top = _nowSingingLabel.Bottom;
        var bottom = _nextLineLabel.Top;
        var height = Math.Max(0, bottom - top);

        _visualizer.SetBounds(0, top, ClientSize.Width, height);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F11:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Keys.Escape when FormBorderStyle == FormBorderStyle.None:
                ToggleFullscreen();
                e.Handled = true;
                break;
        }
    }

    private void ToggleFullscreen()
    {
        if (FormBorderStyle == FormBorderStyle.None)
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            _hintLabel.Visible = true;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            _hintLabel.Visible = false;
        }

        RepositionVisualizer();
    }
}
