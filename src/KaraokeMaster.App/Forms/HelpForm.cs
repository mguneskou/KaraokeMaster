using KaraokeMaster.App.Theming;

namespace KaraokeMaster.App.Forms;

/// <summary>
/// A single scrollable how-to-use guide, since the app has no online help and everything
/// (vocal separation, auto-lyrics, mixer, playlists, the Performer window) is otherwise
/// discoverable only by poking around the UI.
/// </summary>
public sealed class HelpForm : Form
{
    private readonly RichTextBox _body = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        WordWrap = true,
        Font = new Font("Segoe UI", 10f),
        Margin = new Padding(12),
    };

    public HelpForm()
    {
        Text = "KaraokeMaster Help";
        Width = 720;
        Height = 680;
        MinimumSize = new Size(480, 360);
        StartPosition = FormStartPosition.CenterParent;
        Icon = AppIcon.TryLoad() ?? Icon;

        _body.BackColor = UiColors.WindowBackground;
        _body.ForeColor = UiColors.TextPrimary;

        var closeButton = new Button { Text = "Close", Width = 90, DialogResult = DialogResult.OK };
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44,
            Padding = new Padding(8),
        };
        buttonPanel.Controls.Add(closeButton);

        var bodyHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        bodyHost.Controls.Add(_body);

        Controls.Add(bodyHost);
        Controls.Add(buttonPanel);

        AcceptButton = closeButton;
        CancelButton = closeButton;

        UiTheme.Apply(this);
        _body.BackColor = UiColors.WindowBackground;
        _body.ForeColor = UiColors.TextPrimary;

        BuildContent();
    }

    private void BuildContent()
    {
        AppendTitle("KaraokeMaster — How to Use");

        AppendHeading("1. Add your music");
        AppendBullet("File → Manage Watched Folders… → Add, then pick a folder of songs.");
        AppendBullet("Supported formats: mp3, flac, wav, m4a.");
        AppendBullet("The Library grid updates automatically whenever a watched folder's contents change — no manual rescan needed.");
        AppendBullet("Double-click any song in the Library to load and start playing it right away.");

        AppendHeading("2. The Library grid");
        AppendBullet("The search box filters by title or artist as you type.");
        AppendBullet("Columns: Title, Artist, Duration, Separation (vocal-separation status), Lyrics (auto-lyrics status).");
        AppendBullet("Right-click a song for: Add to Playlist, Separate Vocals, Generate Lyrics.");

        AppendHeading("3. Vocal separation (instrumental / karaoke track)");
        AppendBullet("Splits a song into an instrumental track and an isolated vocals track using Demucs, an AI model that runs entirely offline on your CPU — nothing is uploaded anywhere.");
        AppendBullet("The first time you use it, you'll be prompted for a one-time setup that downloads Python packages including PyTorch (several gigabytes). This only happens once per machine.");
        AppendBullet("Once a song's Separation status reads \"Ready\", double-clicking it plays the instrumental automatically instead of the original mix.");
        AppendBullet("Separation runs in the background; watch the status bar or the Separation column for progress, and hover a \"Failed\" status for the error.");

        AppendHeading("4. Auto-generated, word-synced lyrics");
        AppendBullet("Uses faster-whisper, a local speech-to-text model, to transcribe the isolated vocals track and produce word-by-word synced lyrics — fully offline, no lyrics API or internet lookup.");
        AppendBullet("Requires vocal separation to be \"Ready\" first, since it transcribes the isolated vocals track.");
        AppendBullet("The first time you use it, you'll be prompted for a one-time setup that downloads faster-whisper and a speech model (a few hundred MB).");
        AppendBullet("Once generated, the Performer window highlights each word gold as it's sung, in sync with playback — just like a real karaoke machine.");

        AppendHeading("5. Now Playing / Mixer");
        AppendBullet("Transport: Play/Pause, Stop, seek bar, volume.");
        AppendBullet("Key: shifts pitch up or down in semitones without changing speed — useful for matching a singer's vocal range.");
        AppendBullet("Tempo: speeds up or slows down playback (%) independently of pitch.");
        AppendBullet("Output device: choose which speaker/audio device plays the mix.");
        AppendBullet("Mic input device + the Mic toggle button: turns on live microphone mixing into the output, so the singer is heard over the instrumental.");
        AppendBullet("Mic Gain: microphone volume.");
        AppendBullet("Echo: a classic karaoke-style vocal echo — Wet (how much echo is mixed in), Fdbk (how many repeats), Delay (time between repeats).");

        AppendHeading("6. Playlists & Up Next queue");
        AppendBullet("Create, rename, or delete playlists with the buttons above the queue grid.");
        AppendBullet("Add a song to the current playlist from its right-click menu in the Library (Add to Playlist).");
        AppendBullet("Drag rows within the queue to reorder them.");
        AppendBullet("Type a name into the Singer column to assign who's up next — it's shown on the Performer window once that entry plays.");
        AppendBullet("Double-click a queue row (outside the Singer cell) to play it immediately.");

        AppendHeading("7. The Performer window (for your TV or second monitor)");
        AppendBullet("View → Open Performer Window.");
        AppendBullet("Drag it onto your TV or second monitor, then press F11 for a clean fullscreen display (Esc to exit fullscreen).");
        AppendBullet("Shows the current lyric line with word-by-word gold highlighting synced to playback, a preview of the next line, and a live audio equalizer.");
        AppendBullet("Opening the window while a song is already playing automatically syncs it to what's currently loaded — no need to replay the song.");

        AppendHeading("A note on privacy");
        AppendBullet("Vocal separation and lyrics generation both run entirely on your own machine. No audio or lyrics are ever sent anywhere.");

        _body.Select(0, 0);
    }

    private void AppendTitle(string text)
    {
        AppendLine(text, new Font(_body.Font.FontFamily, 16f, FontStyle.Bold), UiColors.Accent);
        _body.AppendText(Environment.NewLine);
    }

    private void AppendHeading(string text)
    {
        AppendLine(text, new Font(_body.Font.FontFamily, 12f, FontStyle.Bold), UiColors.Accent);
    }

    private void AppendBullet(string text) => AppendLine("•  " + text, _body.Font, UiColors.TextPrimary);

    private void AppendLine(string text, Font font, Color color)
    {
        _body.SelectionStart = _body.TextLength;
        _body.SelectionLength = 0;
        _body.SelectionFont = font;
        _body.SelectionColor = color;
        _body.AppendText(text + Environment.NewLine);
    }
}
