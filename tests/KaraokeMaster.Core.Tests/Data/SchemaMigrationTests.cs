using Dapper;
using KaraokeMaster.Core.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace KaraokeMaster.Core.Tests.Data;

public class SchemaMigrationTests : IDisposable
{
    private readonly string _dbPath;

    public SchemaMigrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"karaoke-migration-test-{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public void OpeningFreshDatabase_IncludesLyricsColumns()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        using var connection = factory.CreateOpenConnection();

        var columns = connection.Query<string>("SELECT name FROM pragma_table_info('Songs')").ToList();

        Assert.Contains("LyricsStatus", columns);
        Assert.Contains("LyricsError", columns);
    }

    [Fact]
    public void OpeningPreExistingDatabaseWithoutLyricsColumns_AddsThemWithoutDataLoss()
    {
        // Simulate a database created before LyricsStatus/LyricsError existed (an older schema
        // version, same shape a real user's pre-upgrade database would have).
        const string oldSchema = """
            CREATE TABLE Songs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FilePath TEXT NOT NULL UNIQUE,
                Title TEXT NOT NULL,
                Artist TEXT NULL,
                Album TEXT NULL,
                DurationMs INTEGER NOT NULL,
                DateAdded TEXT NOT NULL,
                SeparationStatus TEXT NOT NULL DEFAULT 'NotStarted',
                InstrumentalPath TEXT NULL,
                VocalsPath TEXT NULL,
                LrcPath TEXT NULL,
                SeparationError TEXT NULL
            );
            """;

        using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
        {
            connection.Open();
            connection.Execute(oldSchema);
            connection.Execute(
                "INSERT INTO Songs (FilePath, Title, DurationMs, DateAdded) VALUES ('a.mp3', 'Existing Song', 1000, @dateAdded)",
                new { dateAdded = DateTimeOffset.UtcNow.ToString("O") });
        }
        SqliteConnection.ClearAllPools();

        // Opening via the factory (as the app does on every startup) should migrate the existing
        // table in place, without touching the row already there.
        var factory = new SqliteConnectionFactory(_dbPath);
        using var migratedConnection = factory.CreateOpenConnection();

        var columns = migratedConnection.Query<string>("SELECT name FROM pragma_table_info('Songs')").ToList();
        Assert.Contains("LyricsStatus", columns);
        Assert.Contains("LyricsError", columns);

        var title = migratedConnection.ExecuteScalar<string>("SELECT Title FROM Songs WHERE FilePath = 'a.mp3'");
        Assert.Equal("Existing Song", title);
    }

    [Fact]
    public void ReopeningAlreadyMigratedDatabase_DoesNotThrow()
    {
        _ = new SqliteConnectionFactory(_dbPath);

        var exception = Record.Exception(() => new SqliteConnectionFactory(_dbPath));

        Assert.Null(exception);
    }
}
