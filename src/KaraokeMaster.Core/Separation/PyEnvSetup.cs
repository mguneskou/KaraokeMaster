using System.Diagnostics;
using KaraokeMaster.Core.Processes;

namespace KaraokeMaster.Core.Separation;

/// <summary>
/// Creates and provisions the isolated Python venv (under data\pyenv) that Demucs runs from,
/// so the app never touches a system-wide Python install.
/// </summary>
public sealed class PyEnvSetup
{
    private readonly string _venvRoot;

    public PyEnvSetup() : this(AppPaths.PythonEnvRoot)
    {
    }

    public PyEnvSetup(string venvRoot)
    {
        _venvRoot = venvRoot;
    }

    public string VenvPythonExecutable => Path.Combine(_venvRoot, "Scripts", "python.exe");

    /// <summary>
    /// pip's own download cache defaults to %LOCALAPPDATA%\pip\Cache (on C:), separate from
    /// wherever the venv itself lives. Redirecting it here keeps the whole install — cache
    /// included — off C: when the venv root is on D:.
    /// </summary>
    public string PipCacheDir => Path.Combine(Path.GetDirectoryName(_venvRoot) ?? _venvRoot, "pip-cache");

    public bool IsVenvPresent => File.Exists(VenvPythonExecutable);

    public async Task<bool> IsDemucsInstalledAsync()
    {
        if (!IsVenvPresent)
        {
            return false;
        }

        // Checking bare "import demucs" isn't enough: it can succeed even when a submodule the
        // actual CLI needs (demucs.separate, which pulls in numpy via apply.py/transformer.py)
        // is broken — that gap let a numpy-less venv silently report itself as "ready".
        var (exitCode, _) = await RunAsync(VenvPythonExecutable, "-c \"import demucs.separate\"");
        return exitCode == 0;
    }

    public async Task<bool> IsWhisperInstalledAsync()
    {
        if (!IsVenvPresent)
        {
            return false;
        }

        var (exitCode, _) = await RunAsync(VenvPythonExecutable, "-c \"import faster_whisper\"");
        return exitCode == 0;
    }

    public async Task RunSetupAsync(IProgress<string> progress, CancellationToken cancellationToken = default)
    {
        await EnsureVenvAsync(progress, cancellationToken);

        progress.Report($"Using pip cache at {PipCacheDir} (kept off the C: drive).");

        progress.Report("Upgrading pip...");
        await RunPipStreamingAsync("install --upgrade pip", progress, cancellationToken);

        progress.Report("Installing PyTorch (CPU build) — this is a large download and may take a while...");
        await RunPipStreamingAsync("install torch --index-url https://download.pytorch.org/whl/cpu", progress, cancellationToken);

        // Installed together (not as separate pip calls) so the resolver considers both at once:
        // demucs's own metadata doesn't reliably pull in numpy on its own.
        progress.Report("Installing demucs...");
        await RunPipStreamingAsync("install demucs numpy", progress, cancellationToken);

        progress.Report("Setup complete.");
    }

    /// <summary>
    /// Installs faster-whisper (speech-to-text for auto-generated lyrics) into the same venv used
    /// for vocal separation. Kept as its own setup step, separate from <see cref="RunSetupAsync"/>,
    /// so separation-only users aren't forced through this extra download too. The actual Whisper
    /// model weights download on first real use, not here — see LyricsRunner's HUGGINGFACE_HUB_CACHE.
    /// </summary>
    public async Task RunLyricsSetupAsync(IProgress<string> progress, CancellationToken cancellationToken = default)
    {
        await EnsureVenvAsync(progress, cancellationToken);

        progress.Report($"Using pip cache at {PipCacheDir} (kept off the C: drive).");
        progress.Report("Installing faster-whisper (speech-to-text engine for auto lyrics)...");
        await RunPipStreamingAsync("install faster-whisper", progress, cancellationToken);

        progress.Report("Setup complete. The Whisper model itself downloads the first time you generate lyrics.");
    }

    private async Task EnsureVenvAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (!IsVenvPresent)
        {
            var systemPython = PythonLocator.FindSystemPython()
                ?? throw new InvalidOperationException(
                    "No Python 3 installation found. Install Python 3.10+ from python.org (make sure to enable the 'py' launcher) and try again.");

            progress.Report($"Using system Python: {systemPython}");
            progress.Report($"Creating virtual environment at {_venvRoot}...");
            await RunStreamingAsync(systemPython, $"-m venv \"{_venvRoot}\"", progress, cancellationToken);
        }
        else
        {
            progress.Report("Virtual environment already exists, reusing it.");
        }
    }

    private Task RunPipStreamingAsync(string pipArgs, IProgress<string> progress, CancellationToken cancellationToken) =>
        RunStreamingAsync(VenvPythonExecutable, $"-m pip {pipArgs} --cache-dir \"{PipCacheDir}\"", progress, cancellationToken);

    private static async Task RunStreamingAsync(
        string fileName,
        string arguments,
        IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = PumpAsync(process.StandardOutput, progress, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, progress, cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"'{fileName} {arguments}' exited with code {process.ExitCode}.");
        }
    }

    private static async Task PumpAsync(TextReader reader, IProgress<string> progress, CancellationToken cancellationToken)
    {
        await foreach (var line in ProcessLineReader.ReadLinesAsync(reader, cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                progress.Report(line);
            }
        }
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, output);
    }
}
