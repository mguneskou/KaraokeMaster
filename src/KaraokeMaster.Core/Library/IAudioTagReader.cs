namespace KaraokeMaster.Core.Library;

public sealed record AudioTags(string Title, string? Artist, string? Album, int DurationMs);

public interface IAudioTagReader
{
    AudioTags Read(string filePath);
}
