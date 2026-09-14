using System.Diagnostics;
using System.Text.RegularExpressions;
using KaraokeMaster.Core.Processes;
using KaraokeMaster.Core.Separation;

namespace KaraokeMaster.Core.Lyrics;

/// <summary>Runs the bundled Whisper transcription script against an isolated vocals track via the provisioned venv.</summary>
public sealed partial class LyricsRunner(PyEnvSetup pyEnvSetup)
{
    public const string DefaultModelSize = "small";

    public async Task RunAsync(
        string vocalsWavPath,
        string outputLrcPath,
        IProgress<int>? progressPercent,
        IProgress<string>? logLine,
        string modelSize = DefaultModelSize,
        CancellationToken cancellationToken = default)
    {
        var arguments = $"\"{AppPaths.TranscribeLyricsScriptPath}\" \"{vocalsWavPath}\" \"{outputLrcPath}\" {modelSize}";
        var psi = new ProcessStartInfo(pyEnvSetup.VenvPythonExecutable, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Keeps the Whisper model download off C: — same reasoning as pip's --cache-dir already
        // applied to Demucs's installs (see PyEnvSetup.PipCacheDir).
        psi.Environment["HUGGINGFACE_HUB_CACHE"] = AppPaths.HuggingFaceCacheRoot;

        using var process = new Process { StartInfo = psi };
        process.Start();

        var capturedLines = new List<string>();
        var captureLock = new object();

        var stdoutTask = PumpAsync(process.StandardOutput, progressPercent, logLine, capturedLines, captureLock, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, progressPercent, logLine, capturedLines, captureLock, cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask);

        if (process.ExitCode != 0)
        {
            string tail;
            lock (captureLock)
            {
                tail = string.Join(" | ", capturedLines.TakeLast(8));
            }

            var detail = string.IsNullOrWhiteSpace(tail) ? "(no output captured)" : tail;
            throw new InvalidOperationException($"lyrics transcription exited with code {process.ExitCode}: {detail}");
        }

        if (!File.Exists(outputLrcPath))
        {
            throw new InvalidOperationException($"transcription completed but no .lrc was produced at {outputLrcPath}.");
        }
    }

    private static async Task PumpAsync(
        TextReader reader,
        IProgress<int>? progressPercent,
        IProgress<string>? logLine,
        List<string> capturedLines,
        object captureLock,
        CancellationToken cancellationToken)
    {
        await foreach (var line in ProcessLineReader.ReadLinesAsync(reader, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            logLine?.Report(line);
            lock (captureLock)
            {
                capturedLines.Add(line);
            }

            if (TryParsePercent(line, out var percent))
            {
                progressPercent?.Report(percent);
            }
        }
    }

    /// <summary>Extracts the "NN" from a "PROGRESS NN" line the transcribe_lyrics.py script prints.</summary>
    public static bool TryParsePercent(string line, out int percent)
    {
        var match = ProgressRegex().Match(line);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
        {
            percent = Math.Clamp(value, 0, 100);
            return true;
        }

        percent = 0;
        return false;
    }

    [GeneratedRegex(@"^PROGRESS (\d{1,3})$")]
    private static partial Regex ProgressRegex();
}
