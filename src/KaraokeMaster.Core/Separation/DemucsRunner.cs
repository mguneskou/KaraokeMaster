using System.Diagnostics;
using System.Text.RegularExpressions;
using KaraokeMaster.Core.Processes;

namespace KaraokeMaster.Core.Separation;

public sealed record SeparationResult(string InstrumentalPath, string VocalsPath);

/// <summary>Runs Demucs (two-stem vocals/instrumental) against a WAV file via the provisioned venv.</summary>
public sealed partial class DemucsRunner(PyEnvSetup pyEnvSetup)
{
    public const string ModelName = "htdemucs";

    public async Task<SeparationResult> RunAsync(
        string inputWavPath,
        string outputRootDir,
        IProgress<int>? progressPercent,
        IProgress<string>? logLine,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputRootDir);

        var arguments = $"-m demucs --two-stems=vocals -n {ModelName} -o \"{outputRootDir}\" \"{inputWavPath}\"";
        var psi = new ProcessStartInfo(pyEnvSetup.VenvPythonExecutable, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Keep every line so a failure's exception message can quote the actual Python error,
        // not just an exit code — that's the only diagnostic that previously reached the DB/UI.
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
            throw new InvalidOperationException($"demucs exited with code {process.ExitCode}: {detail}");
        }

        var stemName = Path.GetFileNameWithoutExtension(inputWavPath);
        var stemDir = Path.Combine(outputRootDir, ModelName, stemName);
        var instrumentalPath = Path.Combine(stemDir, "no_vocals.wav");
        var vocalsPath = Path.Combine(stemDir, "vocals.wav");

        if (!File.Exists(instrumentalPath) || !File.Exists(vocalsPath))
        {
            throw new InvalidOperationException($"demucs completed but expected output was not found in {stemDir}.");
        }

        return new SeparationResult(instrumentalPath, vocalsPath);
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

    /// <summary>Extracts a "NN%" progress figure from a demucs/tqdm output line, if present.</summary>
    public static bool TryParsePercent(string line, out int percent)
    {
        var match = PercentRegex().Match(line);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
        {
            percent = Math.Clamp(value, 0, 100);
            return true;
        }

        percent = 0;
        return false;
    }

    [GeneratedRegex(@"(\d{1,3})%")]
    private static partial Regex PercentRegex();
}
