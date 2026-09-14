using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Models;
using Xunit;

namespace KaraokeMaster.Core.Tests.Data;

public class SongRepositoryTests : SqliteTestBase
{
    private readonly SongRepository _repo;

    public SongRepositoryTests()
    {
        _repo = new SongRepository(Factory);
    }

    private static Song MakeSong(string path, string title = "Test Song", string? artist = "Test Artist") => new()
    {
        FilePath = path,
        Title = title,
        Artist = artist,
        Album = "Test Album",
        DurationMs = 180_000,
        DateAdded = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Upsert_NewSong_CanBeRetrievedById()
    {
        var id = await _repo.UpsertAsync(MakeSong(@"C:\music\song.mp3"));

        var song = await _repo.GetByIdAsync(id);

        Assert.NotNull(song);
        Assert.Equal("Test Song", song!.Title);
        Assert.Equal(SeparationStatus.NotStarted, song.SeparationStatus);
    }

    [Fact]
    public async Task Upsert_SameFilePathTwice_UpdatesExistingRowInsteadOfInserting()
    {
        var song = MakeSong(@"C:\music\song.mp3");
        var firstId = await _repo.UpsertAsync(song);

        song.Title = "Updated Title";
        var secondId = await _repo.UpsertAsync(song);

        Assert.Equal(firstId, secondId);
        var all = await _repo.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Updated Title", all[0].Title);
    }

    [Fact]
    public async Task Search_MatchesTitleOrArtist()
    {
        await _repo.UpsertAsync(MakeSong(@"C:\music\a.mp3", title: "Bohemian Rhapsody", artist: "Queen"));
        await _repo.UpsertAsync(MakeSong(@"C:\music\b.mp3", title: "Another One Bites the Dust", artist: "Queen"));
        await _repo.UpsertAsync(MakeSong(@"C:\music\c.mp3", title: "Imagine", artist: "John Lennon"));

        var byTitle = await _repo.SearchAsync("Bohemian");
        var byArtist = await _repo.SearchAsync("Queen");

        Assert.Single(byTitle);
        Assert.Equal(2, byArtist.Count);
    }

    [Fact]
    public async Task UpdateSeparationStatus_PersistsStatusAndPaths()
    {
        var id = await _repo.UpsertAsync(MakeSong(@"C:\music\d.mp3"));

        await _repo.UpdateSeparationStatusAsync(id, SeparationStatus.Ready, instrumentalPath: "instr.wav", vocalsPath: "vocals.wav");

        var song = await _repo.GetByIdAsync(id);
        Assert.Equal(SeparationStatus.Ready, song!.SeparationStatus);
        Assert.Equal("instr.wav", song.InstrumentalPath);
        Assert.Equal("vocals.wav", song.VocalsPath);
    }

    [Fact]
    public async Task UpdateSeparationStatus_Failed_PersistsErrorMessage()
    {
        var id = await _repo.UpsertAsync(MakeSong(@"C:\music\e.mp3"));

        await _repo.UpdateSeparationStatusAsync(id, SeparationStatus.Failed, error: "demucs exited with code 1");

        var song = await _repo.GetByIdAsync(id);
        Assert.Equal(SeparationStatus.Failed, song!.SeparationStatus);
        Assert.Equal("demucs exited with code 1", song.SeparationError);
    }

    [Fact]
    public async Task Delete_RemovesSong()
    {
        var id = await _repo.UpsertAsync(MakeSong(@"C:\music\f.mp3"));

        await _repo.DeleteAsync(id);

        Assert.Null(await _repo.GetByIdAsync(id));
    }
}
