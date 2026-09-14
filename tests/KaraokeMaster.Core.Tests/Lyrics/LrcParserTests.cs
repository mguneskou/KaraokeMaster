using KaraokeMaster.Core.Lyrics;
using Xunit;

namespace KaraokeMaster.Core.Tests.Lyrics;

public class LrcParserTests
{
    [Fact]
    public void Parse_ExtractsTimedLinesInOrder()
    {
        const string lrc = """
            [ti:Test Song]
            [ar:Test Artist]
            [00:12.34]First line
            [00:05.00]Second line comes earlier
            [01:02.50]Third line
            """;

        var lines = LrcParser.Parse(lrc);

        Assert.Equal(3, lines.Count);
        Assert.Equal(TimeSpan.FromSeconds(5), lines[0].Time);
        Assert.Equal("Second line comes earlier", lines[0].Text);
        Assert.Equal(new TimeSpan(0, 0, 0, 12, 340), lines[1].Time);
        Assert.Equal(new TimeSpan(0, 0, 1, 2, 500), lines[2].Time);
    }

    [Fact]
    public void Parse_IgnoresMetadataAndBlankLines()
    {
        const string lrc = """
            [ti:Some Title]
            [by:whoever]

            [00:01.00]Only real lyric line
            """;

        var lines = LrcParser.Parse(lrc);

        Assert.Single(lines);
        Assert.Equal("Only real lyric line", lines[0].Text);
    }

    [Fact]
    public void Parse_HandlesMultipleTimestampsOnOneLine()
    {
        const string lrc = "[00:10.00][00:20.00]Repeated chorus line";

        var lines = LrcParser.Parse(lrc);

        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal("Repeated chorus line", l.Text));
        Assert.Equal(TimeSpan.FromSeconds(10), lines[0].Time);
        Assert.Equal(TimeSpan.FromSeconds(20), lines[1].Time);
    }

    [Theory]
    [InlineData("[00:01.5]Half second", 500)]
    [InlineData("[00:01.50]Half second", 500)]
    [InlineData("[00:01.500]Half second", 500)]
    public void Parse_NormalizesFractionalSecondsRegardlessOfDigitCount(string line, int expectedMs)
    {
        var lines = LrcParser.Parse(line);

        Assert.Single(lines);
        Assert.Equal(expectedMs, lines[0].Time.Milliseconds);
    }

    [Fact]
    public void FindActiveIndex_ReturnsLastLineAtOrBeforePosition()
    {
        var lines = new[]
        {
            new LrcLine(TimeSpan.FromSeconds(0), "a"),
            new LrcLine(TimeSpan.FromSeconds(10), "b"),
            new LrcLine(TimeSpan.FromSeconds(20), "c"),
        };

        Assert.Equal(-1, LrcParser.FindActiveIndex(lines, TimeSpan.FromSeconds(-1)));
        Assert.Equal(0, LrcParser.FindActiveIndex(lines, TimeSpan.FromSeconds(5)));
        Assert.Equal(1, LrcParser.FindActiveIndex(lines, TimeSpan.FromSeconds(10)));
        Assert.Equal(1, LrcParser.FindActiveIndex(lines, TimeSpan.FromSeconds(19)));
        Assert.Equal(2, LrcParser.FindActiveIndex(lines, TimeSpan.FromSeconds(999)));
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsEmptyList()
    {
        var lines = LrcParser.Parse(string.Empty);

        Assert.Empty(lines);
    }

    [Fact]
    public void Parse_PlainLine_HasNoWords()
    {
        var lines = LrcParser.Parse("[00:01.00]Plain line with no word tags");

        Assert.Empty(lines[0].Words);
    }

    [Fact]
    public void Parse_EnhancedLine_ExtractsPerWordTimingAndCleanJoinedText()
    {
        const string lrc = "[00:12.34]<00:12.34>Merhaba <00:12.80>dünya <00:13.20>nasılsın";

        var lines = LrcParser.Parse(lrc);

        Assert.Single(lines);
        var line = lines[0];
        Assert.Equal(TimeSpan.FromSeconds(12.34), line.Time);
        Assert.Equal("Merhaba dünya nasılsın", line.Text);
        Assert.Equal(3, line.Words.Count);
        Assert.Equal("Merhaba", line.Words[0].Text);
        Assert.Equal(new TimeSpan(0, 0, 0, 12, 340), line.Words[0].Time);
        Assert.Equal("dünya", line.Words[1].Text);
        Assert.Equal(new TimeSpan(0, 0, 0, 12, 800), line.Words[1].Time);
        Assert.Equal("nasılsın", line.Words[2].Text);
        Assert.Equal(new TimeSpan(0, 0, 0, 13, 200), line.Words[2].Time);
    }

    [Fact]
    public void Parse_EnhancedLine_LineTimeMatchesFirstWordTime()
    {
        // The line's own leading [mm:ss.xx] tag should agree with the first <mm:ss.xx> word tag,
        // as our own generator always writes them, but the parser shouldn't assume they're forced
        // to match — it should just trust each tag independently.
        const string lrc = "[00:12.34]<00:12.50>Slightly <00:13.00>later";

        var lines = LrcParser.Parse(lrc);

        Assert.Equal(TimeSpan.FromSeconds(12.34), lines[0].Time);
        Assert.Equal(new TimeSpan(0, 0, 0, 12, 500), lines[0].Words[0].Time);
    }

    [Fact]
    public void FindActiveWordIndex_ReturnsLastWordAtOrBeforePosition()
    {
        var words = new[]
        {
            new LrcWord(TimeSpan.FromSeconds(0), "one"),
            new LrcWord(TimeSpan.FromSeconds(1), "two"),
            new LrcWord(TimeSpan.FromSeconds(2), "three"),
        };

        Assert.Equal(-1, LrcParser.FindActiveWordIndex(words, TimeSpan.FromSeconds(-1)));
        Assert.Equal(0, LrcParser.FindActiveWordIndex(words, TimeSpan.FromSeconds(0.5)));
        Assert.Equal(1, LrcParser.FindActiveWordIndex(words, TimeSpan.FromSeconds(1)));
        Assert.Equal(2, LrcParser.FindActiveWordIndex(words, TimeSpan.FromSeconds(999)));
    }

    [Fact]
    public void FindActiveWordIndex_EmptyWordList_ReturnsNegativeOne()
    {
        Assert.Equal(-1, LrcParser.FindActiveWordIndex(Array.Empty<LrcWord>(), TimeSpan.FromSeconds(5)));
    }
}
