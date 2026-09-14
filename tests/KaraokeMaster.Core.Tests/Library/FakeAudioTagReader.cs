using KaraokeMaster.Core.Library;

namespace KaraokeMaster.Core.Tests.Library;

/// <summary>Returns canned tags derived from the file name so tests don't need real audio binaries.</summary>
internal sealed class FakeAudioTagReader : IAudioTagReader
{
    public AudioTags Read(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        return new AudioTags($"{name} Title", $"{name} Artist", "Fake Album", 200_000);
    }
}
