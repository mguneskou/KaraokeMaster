using KaraokeMaster.Core.Lyrics;
using Xunit;

namespace KaraokeMaster.Core.Tests.Lyrics;

public class LyricsRunnerTests
{
    [Theory]
    [InlineData("PROGRESS 0", 0)]
    [InlineData("PROGRESS 42", 42)]
    [InlineData("PROGRESS 100", 100)]
    public void TryParsePercent_ExtractsPercentFromProgressLine(string line, int expected)
    {
        var success = LyricsRunner.TryParsePercent(line, out var percent);

        Assert.True(success);
        Assert.Equal(expected, percent);
    }

    [Theory]
    [InlineData("Loading Whisper model 'small'...")]
    [InlineData("Detected language: tr (p=0.98)")]
    [InlineData("")]
    [InlineData("some PROGRESS 50 embedded mid-line")]
    public void TryParsePercent_ReturnsFalseForNonProgressLines(string line)
    {
        var success = LyricsRunner.TryParsePercent(line, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParsePercent_ClampsOutOfRangeValues()
    {
        var success = LyricsRunner.TryParsePercent("PROGRESS 999", out var percent);

        Assert.True(success);
        Assert.Equal(100, percent);
    }
}
