using KaraokeMaster.App.Theming;
using KaraokeMaster.Core;
using KaraokeMaster.Core.Separation;

namespace KaraokeMaster.App.Forms;

public sealed class SetupWizardForm : Form
{
    public enum SetupKind
    {
        Separation,
        Lyrics,
    }

    private readonly PyEnvSetup _pyEnvSetup;
    private readonly SetupKind _kind;

    private readonly Label _infoLabel = new() { Dock = DockStyle.Top, Height = 70 };

    private readonly TextBox _logBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font(FontFamily.GenericMonospace, 9f),
        BackColor = Color.Black,
        ForeColor = Color.LightGreen,
    };

    private readonly Button _startButton = new() { Text = "Start Setup", Dock = DockStyle.Bottom, Height = 32 };

    public bool SetupSucceeded { get; private set; }

    public SetupWizardForm(PyEnvSetup pyEnvSetup, SetupKind kind = SetupKind.Separation)
    {
        _pyEnvSetup = pyEnvSetup;
        _kind = kind;

        Text = kind == SetupKind.Separation ? "Set Up Vocal Separation" : "Set Up Auto Lyrics";
        _infoLabel.Text = kind == SetupKind.Separation
            ? "Vocal separation uses Demucs (a Python/PyTorch tool) to split songs into instrumental and " +
              "vocal tracks. This one-time setup downloads Python packages into " +
              $"{AppPaths.PythonEnvRoot} — mostly PyTorch, a multi-gigabyte download. It can take several " +
              "minutes depending on your connection, and only needs to run once."
            : "Auto lyrics uses faster-whisper (a local speech-recognition engine) to transcribe a song's " +
              "isolated vocals into word-timed lyrics — no internet lookup, nothing leaves your machine. " +
              "This one-time setup installs the package into the same environment vocal separation uses; " +
              "the Whisper model itself (a few hundred MB) downloads the first time you actually generate lyrics.";

        Width = 640;
        Height = 480;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;

        Controls.Add(_logBox);
        Controls.Add(_startButton);
        Controls.Add(_infoLabel);

        UiTheme.Apply(this);

        _startButton.Click += async (_, _) => await RunSetupAsync();
    }

    private async Task RunSetupAsync()
    {
        _startButton.Enabled = false;
        _logBox.Clear();

        var progress = new Progress<string>(line => _logBox.AppendText(line + Environment.NewLine));

        try
        {
            if (_kind == SetupKind.Separation)
            {
                await _pyEnvSetup.RunSetupAsync(progress);
            }
            else
            {
                await _pyEnvSetup.RunLyricsSetupAsync(progress);
            }

            SetupSucceeded = true;
            _logBox.AppendText(Environment.NewLine + "Setup finished successfully. You can close this window.");
        }
        catch (Exception ex)
        {
            _logBox.AppendText(Environment.NewLine + $"Setup failed: {ex.Message}");
        }
        finally
        {
            _startButton.Enabled = true;
        }
    }
}
