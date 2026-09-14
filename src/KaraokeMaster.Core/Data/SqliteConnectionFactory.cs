using Dapper;
using KaraokeMaster.Core.Models;
using Microsoft.Data.Sqlite;

namespace KaraokeMaster.Core.Data;

public sealed class SqliteConnectionFactory
{
    private readonly string _dbPath;

    static SqliteConnectionFactory()
    {
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<SeparationStatus>());
        SqlMapper.AddTypeHandler(new DateTimeOffsetStringTypeHandler());
    }

    public SqliteConnectionFactory() : this(AppPaths.DatabaseFile)
    {
    }

    public SqliteConnectionFactory(string dbPath)
    {
        _dbPath = dbPath;
        using var connection = CreateOpenConnection();
        connection.Execute(SchemaSql.CreateTables);
        ApplyMigrations(connection);
    }

    public SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        connection.Execute("PRAGMA foreign_keys = ON;");
        return connection;
    }

    /// <summary>
    /// Additive-only schema evolution for columns added after a database may already exist on a
    /// user's machine — CREATE TABLE IF NOT EXISTS is a no-op against an existing table, so new
    /// columns need an explicit, idempotent ALTER TABLE check instead.
    /// </summary>
    private static void ApplyMigrations(SqliteConnection connection)
    {
        EnsureColumn(connection, "Songs", "LyricsStatus", "TEXT NOT NULL DEFAULT 'NotStarted'");
        EnsureColumn(connection, "Songs", "LyricsError", "TEXT NULL");
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string columnDefinition)
    {
        var existingColumns = connection.Query<string>($"SELECT name FROM pragma_table_info('{table}')");
        if (!existingColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
        {
            connection.Execute($"ALTER TABLE {table} ADD COLUMN {column} {columnDefinition}");
        }
    }
}
