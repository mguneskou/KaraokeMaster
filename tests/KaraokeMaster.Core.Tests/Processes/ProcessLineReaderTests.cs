using KaraokeMaster.Core.Processes;
using Xunit;

namespace KaraokeMaster.Core.Tests.Processes;

public class ProcessLineReaderTests
{
    [Fact]
    public async Task ReadLinesAsync_SplitsOnNewlines()
    {
        using var reader = new StringReader("line1\nline2\nline3");

        var lines = await CollectAsync(reader);

        Assert.Equal(["line1", "line2", "line3"], lines);
    }

    [Fact]
    public async Task ReadLinesAsync_SplitsOnCarriageReturns_LikeTqdmProgressRedraws()
    {
        using var reader = new StringReader(" 10%|...\r 50%|...\r100%|...\r");

        var lines = await CollectAsync(reader);

        Assert.Equal([" 10%|...", " 50%|...", "100%|..."], lines);
    }

    [Fact]
    public async Task ReadLinesAsync_HandlesMixedNewlineStyles()
    {
        using var reader = new StringReader("a\r\nb\nc\rd");

        var lines = await CollectAsync(reader);

        Assert.Equal(["a", "b", "c", "d"], lines);
    }

    [Fact]
    public async Task ReadLinesAsync_EmptyStream_YieldsNoLines()
    {
        using var reader = new StringReader("");

        var lines = await CollectAsync(reader);

        Assert.Empty(lines);
    }

    private static async Task<List<string>> CollectAsync(TextReader reader)
    {
        var lines = new List<string>();
        await foreach (var line in ProcessLineReader.ReadLinesAsync(reader))
        {
            lines.Add(line);
        }
        return lines;
    }
}
