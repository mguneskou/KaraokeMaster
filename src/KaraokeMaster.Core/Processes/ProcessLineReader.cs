using System.Runtime.CompilerServices;
using System.Text;

namespace KaraokeMaster.Core.Processes;

/// <summary>
/// Reads a stream as a sequence of "lines" split on either \n or \r, since tools like pip and
/// demucs use \r to redraw a progress bar in place rather than emitting a new line per update.
/// Shared by any subprocess runner (Demucs, lyrics transcription, ...) that streams progress.
/// </summary>
public static class ProcessLineReader
{
    public static async IAsyncEnumerable<string> ReadLinesAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new StringBuilder();
        var charBuffer = new char[1];

        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await reader.ReadAsync(charBuffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            var c = charBuffer[0];
            if (c is '\n' or '\r')
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }
            }
            else
            {
                buffer.Append(c);
            }
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }
}
