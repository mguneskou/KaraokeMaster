using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Models;

namespace KaraokeMaster.Core.Library;

public sealed record LibraryScanResult(int Added, int Updated, int Removed);

public sealed class LibraryScanner
{
    private static readonly string[] SupportedExtensions = [".mp3", ".flac", ".wav", ".m4a"];

    private readonly SongRepository _songRepository;
    private readonly IAudioTagReader _tagReader;

    public LibraryScanner(SongRepository songRepository) : this(songRepository, new TagLibAudioTagReader())
    {
    }

    public LibraryScanner(SongRepository songRepository, IAudioTagReader tagReader)
    {
        _songRepository = songRepository;
        _tagReader = tagReader;
    }

    public async Task<LibraryScanResult> ScanAsync(IEnumerable<string> folderPaths)
    {
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var updated = 0;

        foreach (var folder in folderPaths)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
            {
                if (!SupportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                seenPaths.Add(file);

                var existing = await _songRepository.GetByFilePathAsync(file);
                var tags = ReadTagsOrFallback(file);
                var lrcPath = Path.ChangeExtension(file, ".lrc");

                var song = new Song
                {
                    FilePath = file,
                    Title = string.IsNullOrWhiteSpace(tags.Title) ? Path.GetFileNameWithoutExtension(file) : tags.Title,
                    Artist = tags.Artist,
                    Album = tags.Album,
                    DurationMs = tags.DurationMs,
                    DateAdded = DateTimeOffset.UtcNow,
                    LrcPath = File.Exists(lrcPath) ? lrcPath : null,
                };
                await _songRepository.UpsertAsync(song);

                if (existing is null)
                {
                    added++;
                }
                else
                {
                    updated++;
                }
            }
        }

        var removed = await PruneMissingAsync(seenPaths);

        return new LibraryScanResult(added, updated, removed);
    }

    private AudioTags ReadTagsOrFallback(string file)
    {
        try
        {
            return _tagReader.Read(file);
        }
        catch
        {
            return new AudioTags(Path.GetFileNameWithoutExtension(file), null, null, 0);
        }
    }

    private async Task<int> PruneMissingAsync(HashSet<string> seenPaths)
    {
        var all = await _songRepository.GetAllAsync();
        var removed = 0;

        foreach (var song in all)
        {
            if (!seenPaths.Contains(song.FilePath) && !File.Exists(song.FilePath))
            {
                await _songRepository.DeleteAsync(song.Id);
                removed++;
            }
        }

        return removed;
    }
}
