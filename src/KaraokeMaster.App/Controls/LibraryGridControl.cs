using KaraokeMaster.Core.Models;

namespace KaraokeMaster.App.Controls;

public sealed class LibraryGridControl : UserControl
{
    private readonly TextBox _searchBox = new()
    {
        Dock = DockStyle.Top,
        PlaceholderText = "Search title or artist...",
    };

    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
    };

    private readonly ContextMenuStrip _contextMenu = new();
    private readonly ToolStripMenuItem _addToPlaylistMenuItem = new("Add to Playlist");
    private readonly ToolStripMenuItem _separateVocalsMenuItem = new("Separate Vocals");
    private readonly ToolStripMenuItem _generateLyricsMenuItem = new("Generate Lyrics");

    private List<Song> _allSongs = [];

    public event EventHandler<Song>? SongActivated;
    public event EventHandler<Song>? SeparateVocalsRequested;
    public event EventHandler<Song>? AddToPlaylistRequested;
    public event EventHandler<Song>? GenerateLyricsRequested;

    public LibraryGridControl()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title", DataPropertyName = "Title", FillWeight = 35 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Artist", HeaderText = "Artist", DataPropertyName = "Artist", FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Duration", DataPropertyName = "DurationDisplay", FillWeight = 13 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Separation", DataPropertyName = "SeparationStatusDisplay", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lyrics", HeaderText = "Lyrics", DataPropertyName = "LyricsStatusDisplay", FillWeight = 15 });

        _contextMenu.Items.Add(_addToPlaylistMenuItem);
        _contextMenu.Items.Add(_separateVocalsMenuItem);
        _contextMenu.Items.Add(_generateLyricsMenuItem);
        _contextMenu.Opening += (_, _) =>
        {
            var song = SelectedSong;
            var separationStatus = song?.SeparationStatus;
            var lyricsStatus = song?.LyricsStatus;

            _separateVocalsMenuItem.Text = separationStatus == SeparationStatus.Failed ? "Retry Separation" : "Separate Vocals";
            _separateVocalsMenuItem.Enabled = separationStatus is not (SeparationStatus.Queued or SeparationStatus.Processing);

            _generateLyricsMenuItem.Text = lyricsStatus == SeparationStatus.Failed ? "Retry Lyrics" : "Generate Lyrics";
            _generateLyricsMenuItem.Enabled = separationStatus == SeparationStatus.Ready
                && lyricsStatus is not (SeparationStatus.Queued or SeparationStatus.Processing);
        };
        _addToPlaylistMenuItem.Click += (_, _) =>
        {
            if (SelectedSong is { } song)
            {
                AddToPlaylistRequested?.Invoke(this, song);
            }
        };
        _separateVocalsMenuItem.Click += (_, _) =>
        {
            if (SelectedSong is { } song)
            {
                SeparateVocalsRequested?.Invoke(this, song);
            }
        };
        _generateLyricsMenuItem.Click += (_, _) =>
        {
            if (SelectedSong is { } song)
            {
                GenerateLyricsRequested?.Invoke(this, song);
            }
        };
        _grid.ContextMenuStrip = _contextMenu;
        _grid.CellMouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                _grid.ClearSelection();
                _grid.Rows[e.RowIndex].Selected = true;
                _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[0];
            }
        };
        _grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not SongRow row)
            {
                return;
            }

            if (e.ColumnIndex == _grid.Columns["Status"]!.Index && row.Song.SeparationStatus == SeparationStatus.Failed)
            {
                e.ToolTipText = string.IsNullOrWhiteSpace(row.Song.SeparationError)
                    ? "Separation failed."
                    : row.Song.SeparationError;
            }
            else if (e.ColumnIndex == _grid.Columns["Lyrics"]!.Index && row.Song.LyricsStatus == SeparationStatus.Failed)
            {
                e.ToolTipText = string.IsNullOrWhiteSpace(row.Song.LyricsError)
                    ? "Lyrics generation failed."
                    : row.Song.LyricsError;
            }
        };

        _searchBox.TextChanged += (_, _) => ApplyFilter();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (_grid.Rows[e.RowIndex].DataBoundItem is SongRow row)
            {
                SongActivated?.Invoke(this, row.Song);
            }
        };

        Controls.Add(_grid);
        Controls.Add(_searchBox);
    }

    public void SetSongs(IReadOnlyList<Song> songs)
    {
        _allSongs = songs.ToList();
        ApplyFilter();
    }

    public Song? SelectedSong => _grid.CurrentRow?.DataBoundItem is SongRow row ? row.Song : null;

    private void ApplyFilter()
    {
        var query = _searchBox.Text;
        IEnumerable<Song> filtered = _allSongs;
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = _allSongs.Where(s =>
                s.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (s.Artist?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _grid.DataSource = filtered.Select(s => new SongRow(s)).ToList();
    }

    private sealed class SongRow(Song song)
    {
        public Song Song { get; } = song;
        public string Title => Song.Title;
        public string? Artist => Song.Artist;

        public string DurationDisplay => TimeSpan.FromMilliseconds(Song.DurationMs)
            .ToString(Song.DurationMs >= 3_600_000 ? @"h\:mm\:ss" : @"m\:ss");

        public string SeparationStatusDisplay => Song.SeparationStatus.ToString();
        public string LyricsStatusDisplay => Song.LyricsStatus.ToString();
    }
}
