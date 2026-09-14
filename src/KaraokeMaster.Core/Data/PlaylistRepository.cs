using Dapper;
using KaraokeMaster.Core.Models;

namespace KaraokeMaster.Core.Data;

public sealed class PlaylistRepository(SqliteConnectionFactory connectionFactory)
{
    public async Task<int> CreateAsync(string name)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO Playlists (Name, CreatedAt) VALUES (@name, @createdAt) RETURNING Id",
            new { name, createdAt = DateTimeOffset.UtcNow });
    }

    public async Task RenameAsync(int id, string name)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        await connection.ExecuteAsync("UPDATE Playlists SET Name = @name WHERE Id = @id", new { id, name });
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        await connection.ExecuteAsync("DELETE FROM Playlists WHERE Id = @id", new { id });
    }

    public async Task<IReadOnlyList<Playlist>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateOpenConnection();
        var rows = await connection.QueryAsync<Playlist>("SELECT * FROM Playlists ORDER BY Name");
        return rows.ToList();
    }

    public async Task<int> AddItemAsync(int playlistId, int songId, string? singerName = null)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        var nextSortOrder = await connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(SortOrder), -1) + 1 FROM PlaylistItems WHERE PlaylistId = @playlistId",
            new { playlistId });
        return await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO PlaylistItems (PlaylistId, SongId, SortOrder, SingerName)
            VALUES (@playlistId, @songId, @nextSortOrder, @singerName)
            RETURNING Id
            """,
            new { playlistId, songId, nextSortOrder, singerName });
    }

    public async Task RemoveItemAsync(int itemId)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        await connection.ExecuteAsync("DELETE FROM PlaylistItems WHERE Id = @itemId", new { itemId });
    }

    public async Task SetSingerNameAsync(int itemId, string? singerName)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        await connection.ExecuteAsync("UPDATE PlaylistItems SET SingerName = @singerName WHERE Id = @itemId", new { itemId, singerName });
    }

    /// <summary>Persists a full reorder: item at index 0 becomes SortOrder 0, etc.</summary>
    public async Task ReorderAsync(IReadOnlyList<int> orderedItemIds)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();
        for (var i = 0; i < orderedItemIds.Count; i++)
        {
            await connection.ExecuteAsync(
                "UPDATE PlaylistItems SET SortOrder = @sortOrder WHERE Id = @id",
                new { sortOrder = i, id = orderedItemIds[i] },
                transaction);
        }
        transaction.Commit();
    }

    public async Task<IReadOnlyList<PlaylistItemView>> GetItemsAsync(int playlistId)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT
                pi.Id, pi.PlaylistId, pi.SongId, pi.SortOrder, pi.SingerName,
                s.Title, s.Artist, s.DurationMs, s.FilePath, s.SeparationStatus, s.InstrumentalPath, s.LrcPath
            FROM PlaylistItems pi
            JOIN Songs s ON s.Id = pi.SongId
            WHERE pi.PlaylistId = @playlistId
            ORDER BY pi.SortOrder
            """;
        var rows = await connection.QueryAsync<PlaylistItemView>(sql, new { playlistId });
        return rows.ToList();
    }
}
