using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Tests.Data;
using CoreLibrary = KaraokeMaster.Core.Library;
using Xunit;

namespace KaraokeMaster.Core.Tests.Library;

public class LibraryScannerTests : SqliteTestBase
{
    private readonly string _musicDir;
    private readonly SongRepository _songs;
    private readonly CoreLibrary.LibraryScanner _scanner;

    public LibraryScannerTests()
    {
        _musicDir = Path.Combine(Path.GetTempPath(), $"karaoke-scan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_musicDir);
        _songs = new SongRepository(Factory);
        _scanner = new CoreLibrary.LibraryScanner(_songs, new FakeAudioTagReader());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_musicDir))
        {
            Directory.Delete(_musicDir, recursive: true);
        }
        base.Dispose(disposing);
    }

    [Fact]
    public async Task ScanAsync_AddsNewSupportedAudioFiles()
    {
        File.WriteAllText(Path.Combine(_musicDir, "song1.mp3"), "");
        File.WriteAllText(Path.Combine(_musicDir, "song2.flac"), "");
        File.WriteAllText(Path.Combine(_musicDir, "notes.txt"), "not audio");

        var result = await _scanner.ScanAsync([_musicDir]);

        Assert.Equal(2, result.Added);
        var songs = await _songs.GetAllAsync();
        Assert.Equal(2, songs.Count);
        Assert.Contains(songs, s => s.Title == "song1 Title" && s.Artist == "song1 Artist");
    }

    [Fact]
    public async Task ScanAsync_RescanningUnchangedFolder_UpdatesInsteadOfDuplicating()
    {
        File.WriteAllText(Path.Combine(_musicDir, "song1.mp3"), "");
        await _scanner.ScanAsync([_musicDir]);

        var result = await _scanner.ScanAsync([_musicDir]);

        Assert.Equal(0, result.Added);
        Assert.Equal(1, result.Updated);
        Assert.Single(await _songs.GetAllAsync());
    }

    [Fact]
    public async Task ScanAsync_FileDeletedFromDisk_IsPrunedFromLibraryOnNextScan()
    {
        var path = Path.Combine(_musicDir, "song1.mp3");
        File.WriteAllText(path, "");
        await _scanner.ScanAsync([_musicDir]);

        File.Delete(path);
        var result = await _scanner.ScanAsync([_musicDir]);

        Assert.Equal(1, result.Removed);
        Assert.Empty(await _songs.GetAllAsync());
    }

    [Fact]
    public async Task ScanAsync_UnsupportedExtension_IsIgnored()
    {
        File.WriteAllText(Path.Combine(_musicDir, "cover.jpg"), "");

        var result = await _scanner.ScanAsync([_musicDir]);

        Assert.Equal(0, result.Added);
        Assert.Empty(await _songs.GetAllAsync());
    }

    [Fact]
    public async Task ScanAsync_NonExistentFolder_IsSkippedWithoutThrowing()
    {
        var result = await _scanner.ScanAsync([Path.Combine(_musicDir, "does-not-exist")]);

        Assert.Equal(0, result.Added);
    }

    [Fact]
    public async Task ScanAsync_SiblingLrcFile_IsDetectedAndLinked()
    {
        File.WriteAllText(Path.Combine(_musicDir, "song1.mp3"), "");
        File.WriteAllText(Path.Combine(_musicDir, "song1.lrc"), "[00:01.00]hello");
        File.WriteAllText(Path.Combine(_musicDir, "song2.mp3"), "");

        await _scanner.ScanAsync([_musicDir]);

        var songs = await _songs.GetAllAsync();
        var withLyrics = Assert.Single(songs, s => s.Title == "song1 Title");
        var withoutLyrics = Assert.Single(songs, s => s.Title == "song2 Title");
        Assert.Equal(Path.Combine(_musicDir, "song1.lrc"), withLyrics.LrcPath);
        Assert.Null(withoutLyrics.LrcPath);
    }

    [Fact]
    public async Task ScanAsync_RescanWithoutLrcFile_DoesNotEraseExistingLrcPath()
    {
        var mp3Path = Path.Combine(_musicDir, "song1.mp3");
        File.WriteAllText(mp3Path, "");
        var lrcPath = Path.Combine(_musicDir, "song1.lrc");
        File.WriteAllText(lrcPath, "[00:01.00]hello");
        await _scanner.ScanAsync([_musicDir]);

        // Simulate an externally-set LrcPath (e.g. a future auto-generated lyrics pass) that
        // doesn't correspond to a sibling file the scanner itself would find.
        var song = await _songs.GetByFilePathAsync(mp3Path);
        await _songs.UpdateLrcPathAsync(song!.Id, @"C:\generated\song1.generated.lrc");
        File.Delete(lrcPath);

        await _scanner.ScanAsync([_musicDir]);

        var rescanned = await _songs.GetByFilePathAsync(mp3Path);
        Assert.Equal(@"C:\generated\song1.generated.lrc", rescanned!.LrcPath);
    }
}
