using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Models;

namespace KaraokeMaster.App.Forms;

public sealed class ManageWatchedFoldersForm : Form
{
    private readonly WatchedFolderRepository _repository;
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };
    private readonly Button _addButton = new() { Text = "Add Folder..." };
    private readonly Button _removeButton = new() { Text = "Remove Selected" };
    private readonly Button _closeButton = new() { Text = "Close", Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };

    private List<WatchedFolder> _folders = [];

    public bool FoldersChanged { get; private set; }

    public ManageWatchedFoldersForm(WatchedFolderRepository repository)
    {
        _repository = repository;
        Text = "Manage Watched Folders";
        Width = 480;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;

        _addButton.Click += async (_, _) => await AddFolderAsync();
        _removeButton.Click += async (_, _) => await RemoveSelectedAsync();

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(4),
        };
        buttonPanel.Controls.Add(_addButton);
        buttonPanel.Controls.Add(_removeButton);

        Controls.Add(_list);
        Controls.Add(buttonPanel);
        Controls.Add(_closeButton);

        AcceptButton = _closeButton;

        Load += async (_, _) => await ReloadAsync();
    }

    private async Task AddFolderAsync()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select a folder to watch for karaoke tracks" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await _repository.AddAsync(dialog.SelectedPath);
            FoldersChanged = true;
            await ReloadAsync();
        }
    }

    private async Task RemoveSelectedAsync()
    {
        if (_list.SelectedIndex < 0)
        {
            return;
        }

        var folder = _folders[_list.SelectedIndex];
        await _repository.RemoveAsync(folder.Id);
        FoldersChanged = true;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _folders = (await _repository.GetAllAsync()).ToList();
        _list.DataSource = null;
        _list.DataSource = _folders.Select(f => f.Path).ToList();
    }
}
