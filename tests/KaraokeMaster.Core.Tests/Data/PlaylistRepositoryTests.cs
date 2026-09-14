using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Models;
using Xunit;

namespace KaraokeMaster.Core.Tests.Data;

public class PlaylistRepositoryTests : SqliteTestBase
{
    private readonly PlaylistRepository _playlists;
    private readonly SongRepository _songs;

    public PlaylistRepositoryTests()
    {
        _playlists = new PlaylistRepository(Factory);
        _songs = new SongRepository(Factory);
    }

    private static Song NewSong(string path) => new()
    {
        FilePath = path,
        Title = Path.GetFileNameWithoutExtension(path),
        DurationMs = 200_000,
        DateAdded = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task AddItem_AppendsWithIncrementingSortOrder()
    {
        var playlistId = await _playlists.CreateAsync("Friday Night");
        var song1 = await _songs.UpsertAsync(NewSong("a.mp3"));
        var song2 = await _songs.UpsertAsync(NewSong("b.mp3"));

        await _playlists.AddItemAsync(playlistId, song1, "Alice");
        await _playlists.AddItemAsync(playlistId, song2, "Bob");

        var items = await _playlists.GetItemsAsync(playlistId);

        Assert.Equal(2, items.Count);
        Assert.Equal(0, items[0].SortOrder);
        Assert.Equal(1, items[1].SortOrder);
        Assert.Equal("Alice", items[0].SingerName);
        Assert.Equal("Bob", items[1].SingerName);
    }

    [Fact]
    public async Task Reorder_ChangesSortOrderToMatchGivenSequence()
    {
        var playlistId = await _playlists.CreateAsync("Party");
        var song1 = await _songs.UpsertAsync(NewSong("a.mp3"));
        var song2 = await _songs.UpsertAsync(NewSong("b.mp3"));
        var item1 = await _playlists.AddItemAsync(playlistId, song1);
        var item2 = await _playlists.AddItemAsync(playlistId, song2);

        await _playlists.ReorderAsync([item2, item1]);

        var items = await _playlists.GetItemsAsync(playlistId);
        Assert.Equal(item2, items[0].Id);
        Assert.Equal(item1, items[1].Id);
    }

    [Fact]
    public async Task RemoveItem_DeletesOnlyThatItem()
    {
        var playlistId = await _playlists.CreateAsync("Party");
        var song1 = await _songs.UpsertAsync(NewSong("a.mp3"));
        var song2 = await _songs.UpsertAsync(NewSong("b.mp3"));
        var item1 = await _playlists.AddItemAsync(playlistId, song1);
        await _playlists.AddItemAsync(playlistId, song2);

        await _playlists.RemoveItemAsync(item1);

        var items = await _playlists.GetItemsAsync(playlistId);
        Assert.Single(items);
        Assert.Equal(song2, items[0].SongId);
    }

    [Fact]
    public async Task SetSingerName_UpdatesExistingItemAfterItWasAdded()
    {
        var playlistId = await _playlists.CreateAsync("Party");
        var songId = await _songs.UpsertAsync(NewSong("a.mp3"));
        var itemId = await _playlists.AddItemAsync(playlistId, songId);

        var items = await _playlists.GetItemsAsync(playlistId);
        Assert.Null(items[0].SingerName);

        await _playlists.SetSingerNameAsync(itemId, "Alex");

        items = await _playlists.GetItemsAsync(playlistId);
        Assert.Equal("Alex", items[0].SingerName);
    }

    [Fact]
    public async Task SetSingerName_EmptyOrNull_ClearsExistingName()
    {
        var playlistId = await _playlists.CreateAsync("Party");
        var songId = await _songs.UpsertAsync(NewSong("a.mp3"));
        var itemId = await _playlists.AddItemAsync(playlistId, songId, "Alex");

        await _playlists.SetSingerNameAsync(itemId, null);

        var items = await _playlists.GetItemsAsync(playlistId);
        Assert.Null(items[0].SingerName);
    }

    [Fact]
    public async Task DeletePlaylist_CascadesToItsItems()
    {
        var playlistId = await _playlists.CreateAsync("Temp");
        var songId = await _songs.UpsertAsync(NewSong("a.mp3"));
        await _playlists.AddItemAsync(playlistId, songId);

        await _playlists.DeleteAsync(playlistId);

        var items = await _playlists.GetItemsAsync(playlistId);
        Assert.Empty(items);
    }
}
