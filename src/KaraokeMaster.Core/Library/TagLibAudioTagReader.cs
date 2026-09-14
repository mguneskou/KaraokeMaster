namespace KaraokeMaster.Core.Library;

public sealed class TagLibAudioTagReader : IAudioTagReader
{
    public AudioTags Read(string filePath)
    {
        using var file = TagLib.File.Create(filePath);

        var title = string.IsNullOrWhiteSpace(file.Tag.Title)
            ? Path.GetFileNameWithoutExtension(filePath)
            : file.Tag.Title;
        var artist = file.Tag.Performers.Length > 0 ? string.Join(", ", file.Tag.Performers) : null;
        var album = string.IsNullOrWhiteSpace(file.Tag.Album) ? null : file.Tag.Album;
        var durationMs = (int)file.Properties.Duration.TotalMilliseconds;

        return new AudioTags(title, artist, album, durationMs);
    }
}
