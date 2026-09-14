using KaraokeMaster.Core.Data;
using Microsoft.Data.Sqlite;

namespace KaraokeMaster.Core.Tests.Data;

public abstract class SqliteTestBase : IDisposable
{
    private readonly string _dbPath;

    protected SqliteConnectionFactory Factory { get; }

    protected SqliteTestBase()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"karaoke-test-{Guid.NewGuid():N}.db");
        Factory = new SqliteConnectionFactory(_dbPath);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
