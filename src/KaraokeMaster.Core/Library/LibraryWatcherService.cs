namespace KaraokeMaster.Core.Library;

/// <summary>
/// Watches a set of folders for file add/remove/rename activity and raises a single debounced
/// event so callers can trigger a rescan without reacting to every individual filesystem event
/// (a single file copy can raise several).
/// </summary>
public sealed class LibraryWatcherService : IDisposable
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(2);

    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Timer _debounceTimer;
    private readonly object _lock = new();
    private bool _pending;

    public event EventHandler? LibraryChanged;

    public LibraryWatcherService()
    {
        _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void SetWatchedFolders(IEnumerable<string> folderPaths)
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
        _watchers.Clear();

        foreach (var folder in folderPaths)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            var watcher = new FileSystemWatcher(folder)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
            };
            watcher.Created += (_, _) => ScheduleRescan();
            watcher.Deleted += (_, _) => ScheduleRescan();
            watcher.Renamed += (_, _) => ScheduleRescan();
            _watchers.Add(watcher);
        }
    }

    private void ScheduleRescan()
    {
        lock (_lock)
        {
            _pending = true;
            _debounceTimer.Change(DebounceDelay, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        lock (_lock)
        {
            if (!_pending)
            {
                return;
            }
            _pending = false;
        }

        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
        _debounceTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
