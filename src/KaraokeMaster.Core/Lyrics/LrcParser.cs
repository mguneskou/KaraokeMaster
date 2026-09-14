using System.Text.RegularExpressions;

namespace KaraokeMaster.Core.Lyrics;

public sealed record LrcWord(TimeSpan Time, string Text);

public sealed record LrcLine(TimeSpan Time, string Text, IReadOnlyList<LrcWord> Words)
{
    /// <summary>Plain line-only lyric, with no per-word timing (ordinary hand-authored .lrc files).</summary>
    public LrcLine(TimeSpan time, string text) : this(time, text, Array.Empty<LrcWord>())
    {
    }
}

/// <summary>
/// Parses .lrc synced-lyrics files: plain lines like "[00:12.34]Some lyric text", and the
/// "enhanced LRC" extension some karaoke tools (and our own Whisper-based auto-generator) use for
/// word-level timing: "[00:12.34]&lt;00:12.34&gt;Some &lt;00:12.80&gt;lyric &lt;00:13.10&gt;text".
/// </summary>
public static partial class LrcParser
{
    public static IReadOnlyList<LrcLine> ParseFile(string path) => Parse(File.ReadAllText(path));

    public static IReadOnlyList<LrcLine> Parse(string content)
    {
        var lines = new List<LrcLine>();

        using var reader = new StringReader(content);
        string? rawLine;
        while ((rawLine = reader.ReadLine()) is not null)
        {
            var lineTagMatches = TimeTagRegex().Matches(rawLine);
            if (lineTagMatches.Count == 0)
            {
                // metadata tag (e.g. [ti:...], [ar:...]) or a blank/plain line — not a timed lyric.
                continue;
            }

            var remainder = TimeTagRegex().Replace(rawLine, string.Empty);
            var (text, words) = ParseWords(remainder);

            // A single line can carry multiple leading timestamps for a repeated lyric, e.g.
            // "[00:12.00][00:45.00]La la la" — emit one entry per timestamp.
            foreach (Match lineTagMatch in lineTagMatches)
            {
                lines.Add(new LrcLine(ParseTimestamp(lineTagMatch), text, words));
            }
        }

        return lines.OrderBy(l => l.Time).ToList();
    }

    /// <summary>Index of the last line whose timestamp has already passed, or -1 before the first line.</summary>
    public static int FindActiveIndex(IReadOnlyList<LrcLine> lines, TimeSpan position)
    {
        var index = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Time > position)
            {
                break;
            }
            index = i;
        }

        return index;
    }

    /// <summary>Same shape as <see cref="FindActiveIndex"/>, one level down at the word level.</summary>
    public static int FindActiveWordIndex(IReadOnlyList<LrcWord> words, TimeSpan position)
    {
        var index = -1;
        for (var i = 0; i < words.Count; i++)
        {
            if (words[i].Time > position)
            {
                break;
            }
            index = i;
        }

        return index;
    }

    private static (string Text, IReadOnlyList<LrcWord> Words) ParseWords(string remainder)
    {
        var wordMatches = WordTagRegex().Matches(remainder);
        if (wordMatches.Count == 0)
        {
            return (remainder.Trim(), Array.Empty<LrcWord>());
        }

        var words = new List<LrcWord>();
        for (var i = 0; i < wordMatches.Count; i++)
        {
            var current = wordMatches[i];
            var segmentStart = current.Index + current.Length;
            var segmentEnd = i + 1 < wordMatches.Count ? wordMatches[i + 1].Index : remainder.Length;
            var wordText = remainder[segmentStart..segmentEnd].Trim();
            if (wordText.Length == 0)
            {
                continue;
            }

            words.Add(new LrcWord(ParseTimestamp(current), wordText));
        }

        return (string.Join(' ', words.Select(w => w.Text)), words);
    }

    private static TimeSpan ParseTimestamp(Match match)
    {
        var minutes = int.Parse(match.Groups[1].Value);
        var seconds = int.Parse(match.Groups[2].Value);
        var fraction = match.Groups[3].Success ? match.Groups[3].Value : "0";

        // Normalize centisecond ("xx") or millisecond ("xxx") precision to milliseconds.
        var milliseconds = fraction.Length switch
        {
            1 => int.Parse(fraction) * 100,
            2 => int.Parse(fraction) * 10,
            _ => int.Parse(fraction),
        };

        return new TimeSpan(0, 0, minutes, seconds, milliseconds);
    }

    [GeneratedRegex(@"\[(\d{1,2}):(\d{2})(?:[.:](\d{1,3}))?\]")]
    private static partial Regex TimeTagRegex();

    [GeneratedRegex(@"<(\d{1,2}):(\d{2})(?:[.:](\d{1,3}))?>")]
    private static partial Regex WordTagRegex();
}
