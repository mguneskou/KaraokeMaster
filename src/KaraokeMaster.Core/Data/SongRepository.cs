using Dapper;
using KaraokeMaster.Core.Models;

namespace KaraokeMaster.Core.Data;

public sealed class SongRepository(SqliteConnectionFactory connectionFactory)
{
    public async Task<int> UpsertAsync(Song song)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        // LrcPath uses COALESCE rather than a straight overwrite: a rescan that finds no sibling
        // .lrc file shouldn't erase a path set another way (e.g. a future auto-generated lyrics pass).
        const string sql = """
            INSERT INTO Songs (FilePath, Title, Artist, Album, DurationMs, DateAdded, SeparationStatus, LrcPath)
            VALUES (@FilePath, @Title, @Artist, @Album, @DurationMs, @DateAdded, @SeparationStatus, @LrcPath)
            ON CONFLICT(FilePath) DO UPDATE SET
                Title = excluded.Title,
                Artist = excluded.Artist,
                Album = excluded.Album,
                DurationMs = excluded.DurationMs,
                LrcPath = COALESCE(excluded.LrcPath, Songs.LrcPath)
            RETURNING Id;
            """;
        return await connection.ExecuteScalarAsync<int>(sql, song);
    }

    public async Task<Song?> GetByIdAsync(int id)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        return await connection.QuerySingleOrDefaultAsync<Song>("SELECT * FROM Songs WHERE Id = @id", new { id });
    }

    public async Task<Song?> GetByFilePathAsync(string filePath)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        return await connection.QuerySingleOrDefaultAsync<Song>("SELECT * FROM Songs WHERE FilePath = @filePath", new { filePath });
    }

    public async Task<IReadOnlyList<Song>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateOpenConnection();
        var rows = await connection.QueryAsync<Song>("SELECT * FROM Songs ORDER BY Title");
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Song>> SearchAsync(string query)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        var like = $"%{query}%";
        var rows = await connection.QueryAsync<Song>(
            "SELECT * FROM Songs WHERE Title LIKE @like OR Artist LIKE @like ORDER BY Title",
            new { like });
        return rows.ToList();
    }

    public async Task UpdateSeparationStatusAsync(
        int id,
        SeparationStatus status,
        string? instrumentalPath = null,
        string? vocalsPath = null,
        string? error = null)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        await connection.ExecuteAsync(
            """
            UPDATE Songs
            SET SeparationStatus = @status, InstrumentalPath = @instrumentalPath, VocalsPath = @vocalsPath, SeparationError = @error
            WHERE Id = @id
            """,
            new { id, status, instrumentalPath, vocalsPath, error });
    }

    public async Task UpdateLrcPathAsync(int id, string? lrcPath)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        await connection.ExecuteAsync("UPDATE Songs SET LrcPath = @lrcPath WHERE Id = @id", new { id, lrcPath });
    }

    public async Task UpdateLyricsStatusAsync(int id, SeparationStatus status, string? lrcPath = null, string? error = null)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        // COALESCE so intermediate Queued/Processing updates (which pass no lrcPath) don't
        // clobber a path set by an earlier successful run.
        await connection.ExecuteAsync(
            """
            UPDATE Songs
            SET LyricsStatus = @status, LrcPath = COALESCE(@lrcPath, LrcPath), LyricsError = @error
            WHERE Id = @id
            """,
            new { id, status, lrcPath, error });
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        await connection.ExecuteAsync("DELETE FROM Songs WHERE Id = @id", new { id });
    }
}
