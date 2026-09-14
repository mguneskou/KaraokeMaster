namespace KaraokeMaster.Core.Models;

public sealed class Playlist
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
