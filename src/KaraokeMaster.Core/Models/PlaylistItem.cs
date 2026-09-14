namespace KaraokeMaster.Core.Models;

public sealed class PlaylistItem
{
    public int Id { get; set; }
    public int PlaylistId { get; set; }
    public int SongId { get; set; }
    public int SortOrder { get; set; }
    public string? SingerName { get; set; }
}

/// <summary>
/// A playlist item joined with the song fields the UI's queue list needs to render a row,
/// so callers don't have to issue a second query per row.
/// </summary>
public sealed class PlaylistItemView
{
    public int Id { get; set; }
    public int PlaylistId { get; set; }
    public int SongId { get; set; }
    public int SortOrder { get; set; }
    public string? SingerName { get; set; }
    public required string Title { get; set; }
    public string? Artist { get; set; }
    public int DurationMs { get; set; }
    public required string FilePath { get; set; }
    public SeparationStatus SeparationStatus { get; set; }
    public string? InstrumentalPath { get; set; }
    public string? LrcPath { get; set; }
}
