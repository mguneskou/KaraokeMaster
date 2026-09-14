namespace KaraokeMaster.Core.Models;

public sealed class Song
{
    public int Id { get; set; }
    public required string FilePath { get; set; }
    public required string Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset DateAdded { get; set; }
    public SeparationStatus SeparationStatus { get; set; } = SeparationStatus.NotStarted;
    public string? InstrumentalPath { get; set; }
    public string? VocalsPath { get; set; }
    public string? LrcPath { get; set; }
    public string? SeparationError { get; set; }
    public SeparationStatus LyricsStatus { get; set; } = SeparationStatus.NotStarted;
    public string? LyricsError { get; set; }
}
