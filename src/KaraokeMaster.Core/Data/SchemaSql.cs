namespace KaraokeMaster.Core.Data;

internal static class SchemaSql
{
    public const string CreateTables = """
        CREATE TABLE IF NOT EXISTS Songs (
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
            SeparationError TEXT NULL,
            LyricsStatus TEXT NOT NULL DEFAULT 'NotStarted',
            LyricsError TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS Playlists (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS PlaylistItems (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PlaylistId INTEGER NOT NULL REFERENCES Playlists(Id) ON DELETE CASCADE,
            SongId INTEGER NOT NULL REFERENCES Songs(Id) ON DELETE CASCADE,
            SortOrder INTEGER NOT NULL,
            SingerName TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_PlaylistItems_PlaylistId ON PlaylistItems(PlaylistId);

        CREATE TABLE IF NOT EXISTS WatchedFolders (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Path TEXT NOT NULL UNIQUE
        );
        """;
}
