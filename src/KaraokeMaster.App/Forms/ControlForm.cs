using KaraokeMaster.App.Controls;
using KaraokeMaster.App.Theming;
using KaraokeMaster.Core;
using KaraokeMaster.Core.Audio;
using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Library;
using KaraokeMaster.Core.Lyrics;
using KaraokeMaster.Core.Models;
using KaraokeMaster.Core.Separation;
using KaraokeMaster.Core.Settings;

namespace KaraokeMaster.App.Forms;

public partial class ControlForm : Form
{
    private readonly SqliteConnectionFactory _connectionFactory = new();
    private readonly SongRepository _songRepository;
    private readonly WatchedFolderRepository _watchedFolderRepository;
    private readonly PlaylistRepository _playlistRepository;
    private readonly LibraryScanner _libraryScanner;
    private readonly LibraryWatcherService _libraryWatcher = new();
    private readonly LibraryGridControl _libraryGrid = new() { Dock = DockStyle.Fill };
    private readonly AudioEngine _audioEngine = new();
    private readonly NowPlayingControl _nowPlayingControl;
    private readonly PlaylistQueueControl _playlistQueueControl;
    private readonly PyEnvSetup _pyEnvSetup = new();
    private readonly SeparationQueue _separationQueue;
    private readonly LyricsQueue _lyricsQueue;
    private readonly SettingsService _settingsService = new();
    private PerformerForm? _performerForm;

    public ControlForm()
    {
        InitializeComponent();
        Icon = AppIcon.TryLoad() ?? Icon;

        _songRepository = new SongRepository(_connectionFactory);
        _watchedFolderRepository = new WatchedFolderRepository(_connectionFactory);
        _playlistRepository = new PlaylistRepository(_connectionFactory);
        _libraryScanner = new LibraryScanner(_songRepository);
        _nowPlayingControl = new NowPlayingControl(_audioEngine) { Dock = DockStyle.Fill };
        _playlistQueueControl = new PlaylistQueueControl(_playlistRepository) { Dock = DockStyle.Fill };
        _separationQueue = new SeparationQueue(_songRepository, _pyEnvSetup);
        _lyricsQueue = new LyricsQueue(_songRepository, _pyEnvSetup);

        libraryGroupBox.Controls.Remove(libraryPlaceholderLabel);
        libraryGroupBox.Controls.Add(_libraryGrid);

        nowPlayingGroupBox.Controls.Remove(nowPlayingPlaceholderLabel);
        nowPlayingGroupBox.Controls.Add(_nowPlayingControl);

        queueGroupBox.Controls.Remove(queuePlaceholderLabel);
        queueGroupBox.Controls.Add(_playlistQueueControl);

        UiTheme.Apply(this);

        _libraryGrid.SongActivated += (_, song) =>
        {
            _nowPlayingControl.LoadAndPlay(song);
            _performerForm?.ShowSong(song);
        };
        _libraryGrid.SeparateVocalsRequested += async (_, song) => await RequestSeparationAsync(song);
        _libraryGrid.GenerateLyricsRequested += async (_, song) => await RequestLyricsAsync(song);
        _libraryGrid.AddToPlaylistRequested += async (_, song) => await _playlistQueueControl.AddSongToCurrentPlaylistAsync(song);

        _playlistQueueControl.SongActivated += async (_, item) => await PlayQueueItemAsync(item);

        manageWatchedFoldersMenuItem.Click += async (_, _) => await OpenManageWatchedFoldersAsync();
        openPerformerWindowMenuItem.Click += (_, _) => OpenPerformerWindow();
        viewHelpMenuItem.Click += (_, _) => OpenHelp();
        _libraryWatcher.LibraryChanged += (_, _) => ScheduleRefreshOnUiThread();
        _separationQueue.ProgressChanged += (_, args) => ScheduleSeparationProgressOnUiThread(args);
        _lyricsQueue.ProgressChanged += (_, args) => ScheduleLyricsProgressOnUiThread(args);

        ApplyWindowSettings();

        Load += async (_, _) =>
        {
            await RefreshLibraryAsync();
            await _playlistQueueControl.ReloadPlaylistsAsync();
        };
        FormClosing += (_, _) => SaveSettings();
        FormClosed += (_, _) =>
        {
            _libraryWatcher.Dispose();
            _audioEngine.Dispose();
            _separationQueue.Dispose();
            _lyricsQueue.Dispose();
            _performerForm?.Close();
        };
    }

    private void ApplyWindowSettings()
    {
        var settings = _settingsService.Load();
        _nowPlayingControl.ApplySettings(settings);

        if (settings is { WindowX: { } x, WindowY: { } y, WindowWidth: { } width, WindowHeight: { } height })
        {
            var bounds = new Rectangle(x, y, width, height);
            var isOnScreen = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(bounds));
            if (isOnScreen)
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = bounds;
            }
        }
    }

    private void SaveSettings()
    {
        var settings = _settingsService.Load();
        _nowPlayingControl.CaptureSettings(settings);

        if (WindowState == FormWindowState.Normal)
        {
            settings.WindowX = Location.X;
            settings.WindowY = Location.Y;
            settings.WindowWidth = Size.Width;
            settings.WindowHeight = Size.Height;
        }

        _settingsService.Save(settings);
    }

    private async Task PlayQueueItemAsync(PlaylistItemView item)
    {
        var song = await _songRepository.GetByIdAsync(item.SongId);
        if (song is null)
        {
            return;
        }

        _nowPlayingControl.LoadAndPlay(song);
        _performerForm?.ShowSong(song, item.SingerName);
    }

    private void OpenPerformerWindow()
    {
        if (_performerForm is { IsDisposed: false })
        {
            _performerForm.Activate();
            return;
        }

        _performerForm = new PerformerForm(_audioEngine);
        _performerForm.FormClosed += (_, _) => _performerForm = null;
        _performerForm.Show(this);

        if (_nowPlayingControl.CurrentSong is { } currentSong)
        {
            _performerForm.ShowSong(currentSong);
        }
    }

    private void OpenHelp()
    {
        using var dialog = new HelpForm();
        dialog.ShowDialog(this);
    }

    private async Task OpenManageWatchedFoldersAsync()
    {
        using var dialog = new ManageWatchedFoldersForm(_watchedFolderRepository);
        dialog.ShowDialog(this);
        if (dialog.FoldersChanged)
        {
            await RefreshLibraryAsync();
        }
    }

    private async Task RequestSeparationAsync(Song song)
    {
        if (!await _pyEnvSetup.IsDemucsInstalledAsync())
        {
            var result = MessageBox.Show(
                this,
                "Vocal separation needs a one-time setup (downloads Python packages, including PyTorch — " +
                "several gigabytes). Set it up now?",
                "Vocal Separation Setup Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            using var wizard = new SetupWizardForm(_pyEnvSetup);
            wizard.ShowDialog(this);
            if (!wizard.SetupSucceeded)
            {
                return;
            }
        }

        _separationQueue.Enqueue(song.Id);
    }

    private async Task RequestLyricsAsync(Song song)
    {
        if (!await _pyEnvSetup.IsWhisperInstalledAsync())
        {
            var result = MessageBox.Show(
                this,
                "Auto-generating lyrics needs a one-time setup (downloads faster-whisper; the speech " +
                "recognition model itself, several hundred MB, downloads the first time you use it). Set it up now?",
                "Lyrics Setup Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            using var wizard = new SetupWizardForm(_pyEnvSetup, SetupWizardForm.SetupKind.Lyrics);
            wizard.ShowDialog(this);
            if (!wizard.SetupSucceeded)
            {
                return;
            }
        }

        _lyricsQueue.Enqueue(song.Id);
    }

    private void ScheduleRefreshOnUiThread()
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(new Action(async () => await RefreshLibraryAsync()));
    }

    private void ScheduleSeparationProgressOnUiThread(SeparationProgressEventArgs args)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(new Action(async () => await HandleSeparationProgressAsync(args)));
    }

    private async Task HandleSeparationProgressAsync(SeparationProgressEventArgs args)
    {
        if (args.Status is SeparationStatus.Ready or SeparationStatus.Failed or SeparationStatus.Queued)
        {
            var songs = await _songRepository.GetAllAsync();
            _libraryGrid.SetSongs(songs);
        }

        dataRootStatusLabel.Text = args.Status switch
        {
            SeparationStatus.Processing when args.PercentComplete is { } percent => $"Separating vocals: {percent}%",
            SeparationStatus.Ready => "Vocal separation complete.",
            SeparationStatus.Failed => $"Vocal separation failed: {args.Message}",
            _ => dataRootStatusLabel.Text,
        };
    }

    private void ScheduleLyricsProgressOnUiThread(LyricsProgressEventArgs args)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(new Action(async () => await HandleLyricsProgressAsync(args)));
    }

    private async Task HandleLyricsProgressAsync(LyricsProgressEventArgs args)
    {
        if (args.Status is SeparationStatus.Ready or SeparationStatus.Failed or SeparationStatus.Queued)
        {
            var songs = await _songRepository.GetAllAsync();
            _libraryGrid.SetSongs(songs);
        }

        dataRootStatusLabel.Text = args.Status switch
        {
            SeparationStatus.Processing when args.PercentComplete is { } percent => $"Generating lyrics: {percent}%",
            SeparationStatus.Ready => "Lyrics generation complete.",
            SeparationStatus.Failed => $"Lyrics generation failed: {args.Message}",
            _ => dataRootStatusLabel.Text,
        };
    }

    private async Task RefreshLibraryAsync()
    {
        var folders = await _watchedFolderRepository.GetAllAsync();
        _libraryWatcher.SetWatchedFolders(folders.Select(f => f.Path));

        if (folders.Count > 0)
        {
            dataRootStatusLabel.Text = "Scanning library...";
            await _libraryScanner.ScanAsync(folders.Select(f => f.Path));
        }

        var songs = await _songRepository.GetAllAsync();
        _libraryGrid.SetSongs(songs);
        dataRootStatusLabel.Text = $"data root: {AppPaths.DataRoot}  |  {songs.Count} song(s) in library";
    }
}
