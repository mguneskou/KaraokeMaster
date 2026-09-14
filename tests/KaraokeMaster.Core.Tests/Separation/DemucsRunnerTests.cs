using KaraokeMaster.Core.Separation;
using Xunit;

namespace KaraokeMaster.Core.Tests.Separation;

public class DemucsRunnerTests
{
    [Theory]
    [InlineData("100%|██████████████████████| 6.0/6.0 [00:02<00:00,  2.85seconds/s]", 100)]
    [InlineData(" 42%|█████████             | 2.5/6.0 [00:01<00:01,  2.50seconds/s]", 42)]
    [InlineData("0%|          | 0/6 [00:00<?, ?it/s]", 0)]
    public void TryParsePercent_ExtractsPercentFromTqdmStyleLine(string line, int expected)
    {
        var success = DemucsRunner.TryParsePercent(line, out var percent);

        Assert.True(success);
        Assert.Equal(expected, percent);
    }

    [Theory]
    [InlineData("Selected model is a bag of 1 models.")]
    [InlineData("Separating track Test Tone.wav")]
    [InlineData("")]
    public void TryParsePercent_ReturnsFalseForNonProgressLines(string line)
    {
        var success = DemucsRunner.TryParsePercent(line, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParsePercent_ClampsOutOfRangeValues()
    {
        var success = DemucsRunner.TryParsePercent("999% done", out var percent);

        Assert.True(success);
        Assert.Equal(100, percent);
    }
}
