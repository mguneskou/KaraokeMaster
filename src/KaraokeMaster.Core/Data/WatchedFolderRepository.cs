using Dapper;
using KaraokeMaster.Core.Models;

namespace KaraokeMaster.Core.Data;

public sealed class WatchedFolderRepository(SqliteConnectionFactory connectionFactory)
{
    public async Task<int> AddAsync(string path)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO WatchedFolders (Path) VALUES (@path) ON CONFLICT(Path) DO UPDATE SET Path = excluded.Path RETURNING Id",
            new { path });
    }

    public async Task RemoveAsync(int id)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        await connection.ExecuteAsync("DELETE FROM WatchedFolders WHERE Id = @id", new { id });
    }

    public async Task<IReadOnlyList<WatchedFolder>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateOpenConnection();
        var rows = await connection.QueryAsync<WatchedFolder>("SELECT * FROM WatchedFolders ORDER BY Path");
        return rows.ToList();
    }
}
