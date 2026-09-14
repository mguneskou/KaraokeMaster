using KaraokeMaster.App.Forms;
using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Models;

namespace KaraokeMaster.App.Controls;

public sealed class PlaylistQueueControl : UserControl
{
    private readonly PlaylistRepository _playlistRepository;

    private readonly ComboBox _playlistCombo = new() { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _newButton = new() { Text = "New", Width = 50 };
    private readonly Button _renameButton = new() { Text = "Rename", Width = 60 };
    private readonly Button _deleteButton = new() { Text = "Delete", Width = 55 };

    private readonly ContextMenuStrip _rowContextMenu = new();
    private readonly ToolStripMenuItem _playMenuItem = new("Play");
    private readonly ToolStripMenuItem _removeMenuItem = new("Remove from Queue");

    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        AllowDrop = true,
    };

    private List<Playlist> _playlists = [];
    private List<PlaylistItemView> _items = [];
    private int _dragRowIndex = -1;

    public event EventHandler<PlaylistItemView>? SongActivated;

    public PlaylistQueueControl(PlaylistRepository playlistRepository)
    {
        _playlistRepository = playlistRepository;

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        topPanel.Controls.Add(_playlistCombo);
        topPanel.Controls.Add(_newButton);
        topPanel.Controls.Add(_renameButton);
        topPanel.Controls.Add(_deleteButton);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title", DataPropertyName = "Title", ReadOnly = true, FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Artist", HeaderText = "Artist", DataPropertyName = "Artist", ReadOnly = true, FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Singer", HeaderText = "Singer", DataPropertyName = "SingerDisplay", ReadOnly = false, FillWeight = 35 });

        _rowContextMenu.Items.Add(_playMenuItem);
        _rowContextMenu.Items.Add(_removeMenuItem);
        _grid.ContextMenuStrip = _rowContextMenu;

        Controls.Add(_grid);
        Controls.Add(topPanel);

        _playlistCombo.SelectedIndexChanged += async (_, _) => await ReloadItemsAsync();
        _newButton.Click += async (_, _) => await CreatePlaylistAsync();
        _renameButton.Click += async (_, _) => await RenamePlaylistAsync();
        _deleteButton.Click += async (_, _) => await DeletePlaylistAsync();

        _grid.CellMouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                _grid.ClearSelection();
                _grid.Rows[e.RowIndex].Selected = true;
                _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[0];
            }
            else if (e.Button == MouseButtons.Left && e.RowIndex >= 0)
            {
                _dragRowIndex = e.RowIndex;
            }
        };
        _grid.MouseMove += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && _dragRowIndex >= 0)
            {
                _grid.DoDragDrop(_dragRowIndex, DragDropEffects.Move);
            }
        };
        _grid.DragOver += (_, e) => e.Effect = DragDropEffects.Move;
        _grid.DragDrop += async (_, e) => await HandleRowDropAsync(e);

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex != _grid.Columns["Singer"]!.Index)
            {
                ActivateRow(e.RowIndex);
            }
        };
        _grid.CellEndEdit += async (_, e) => await PersistSingerNameAsync(e.RowIndex);

        _playMenuItem.Click += (_, _) =>
        {
            if (_grid.CurrentRow is not null)
            {
                ActivateRow(_grid.CurrentRow.Index);
            }
        };
        _removeMenuItem.Click += async (_, _) => await RemoveSelectedAsync();
    }

    public async Task ReloadPlaylistsAsync()
    {
        _playlists = (await _playlistRepository.GetAllAsync()).ToList();

        var previouslySelectedId = (_playlistCombo.SelectedItem as PlaylistItem)?.Id;

        _playlistCombo.Items.Clear();
        foreach (var playlist in _playlists)
        {
            _playlistCombo.Items.Add(new PlaylistItem(playlist.Id, playlist.Name));
        }

        if (_playlistCombo.Items.Count > 0)
        {
            var indexToSelect = previouslySelectedId is null
                ? 0
                : Math.Max(0, _playlists.FindIndex(p => p.Id == previouslySelectedId));
            _playlistCombo.SelectedIndex = indexToSelect;
        }
        else
        {
            await ReloadItemsAsync();
        }
    }

    public async Task AddSongToCurrentPlaylistAsync(Song song)
    {
        if (_playlistCombo.SelectedItem is not PlaylistItem current)
        {
            if (!await PromptCreatePlaylistAsync())
            {
                return;
            }
            current = (PlaylistItem)_playlistCombo.SelectedItem!;
        }

        await _playlistRepository.AddItemAsync(current.Id, song.Id);
        await ReloadItemsAsync();
    }

    private async Task CreatePlaylistAsync() => await PromptCreatePlaylistAsync();

    private async Task<bool> PromptCreatePlaylistAsync()
    {
        using var dialog = new TextInputForm("New Playlist", "Playlist name:");
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.InputText))
        {
            return false;
        }

        await _playlistRepository.CreateAsync(dialog.InputText);
        await ReloadPlaylistsAsync();
        return true;
    }

    private async Task RenamePlaylistAsync()
    {
        if (_playlistCombo.SelectedItem is not PlaylistItem current)
        {
            return;
        }

        using var dialog = new TextInputForm("Rename Playlist", "Playlist name:", current.Name);
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.InputText))
        {
            return;
        }

        await _playlistRepository.RenameAsync(current.Id, dialog.InputText);
        await ReloadPlaylistsAsync();
    }

    private async Task DeletePlaylistAsync()
    {
        if (_playlistCombo.SelectedItem is not PlaylistItem current)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Delete playlist \"{current.Name}\"? This can't be undone.",
            "Delete Playlist",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        await _playlistRepository.DeleteAsync(current.Id);
        await ReloadPlaylistsAsync();
    }

    private async Task ReloadItemsAsync()
    {
        if (_playlistCombo.SelectedItem is not PlaylistItem current)
        {
            _items = [];
            BindGrid();
            return;
        }

        _items = (await _playlistRepository.GetItemsAsync(current.Id)).ToList();
        BindGrid();
    }

    private void BindGrid()
    {
        _grid.DataSource = _items.Select(i => new QueueRow(i)).ToList();
    }

    private async Task HandleRowDropAsync(DragEventArgs e)
    {
        var clientPoint = _grid.PointToClient(new Point(e.X, e.Y));
        var targetIndex = _grid.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

        if (targetIndex < 0 || _dragRowIndex < 0 || targetIndex == _dragRowIndex || _dragRowIndex >= _items.Count)
        {
            _dragRowIndex = -1;
            return;
        }

        var moved = _items[_dragRowIndex];
        _items.RemoveAt(_dragRowIndex);
        _items.Insert(Math.Min(targetIndex, _items.Count), moved);
        _dragRowIndex = -1;

        await _playlistRepository.ReorderAsync(_items.Select(i => i.Id).ToList());
        BindGrid();
    }

    private async Task PersistSingerNameAsync(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _items.Count)
        {
            return;
        }

        if (_grid.Rows[rowIndex].DataBoundItem is not QueueRow row)
        {
            return;
        }

        var singerName = string.IsNullOrWhiteSpace(row.SingerDisplay) ? null : row.SingerDisplay.Trim();
        await _playlistRepository.SetSingerNameAsync(row.Item.Id, singerName);
        _items[rowIndex].SingerName = singerName;
    }

    private void ActivateRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _items.Count)
        {
            return;
        }

        SongActivated?.Invoke(this, _items[rowIndex]);
    }

    private async Task RemoveSelectedAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not QueueRow row)
        {
            return;
        }

        await _playlistRepository.RemoveItemAsync(row.Item.Id);
        await ReloadItemsAsync();
    }

    private sealed record PlaylistItem(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class QueueRow(PlaylistItemView item)
    {
        public PlaylistItemView Item { get; } = item;
        public string Title => Item.Title;
        public string? Artist => Item.Artist;
        public string SingerDisplay { get; set; } = item.SingerName ?? string.Empty;
    }
}
