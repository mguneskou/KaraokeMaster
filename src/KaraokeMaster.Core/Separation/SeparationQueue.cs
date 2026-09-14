using System.Threading.Channels;
using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Models;
using NAudio.Wave;

namespace KaraokeMaster.Core.Separation;

public sealed record SeparationProgressEventArgs(int SongId, SeparationStatus Status, int? PercentComplete = null, string? Message = null);

/// <summary>
/// Background worker that processes songs one at a time: transcode to WAV, run Demucs, persist
/// the resulting stem paths. A single worker loop keeps CPU-heavy separations from piling up
/// concurrently on a personal machine.
/// </summary>
public sealed class SeparationQueue : IDisposable
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    private readonly SongRepository _songRepository;
    private readonly DemucsRunner _demucsRunner;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;

    public event EventHandler<SeparationProgressEventArgs>? ProgressChanged;

    public SeparationQueue(SongRepository songRepository, PyEnvSetup pyEnvSetup)
    {
        _songRepository = songRepository;
        _demucsRunner = new DemucsRunner(pyEnvSetup);
        _workerTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
    }

    public void Enqueue(int songId)
    {
        _songRepository.UpdateSeparationStatusAsync(songId, SeparationStatus.Queued).GetAwaiter().GetResult();
        ProgressChanged?.Invoke(this, new SeparationProgressEventArgs(songId, SeparationStatus.Queued));
        _channel.Writer.TryWrite(songId);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var songId in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                await ProcessSongAsync(songId, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }

    private async Task ProcessSongAsync(int songId, CancellationToken cancellationToken)
    {
        var song = await _songRepository.GetByIdAsync(songId);
        if (song is null)
        {
            return;
        }

        string? tempWavPath = null;
        try
        {
            await _songRepository.UpdateSeparationStatusAsync(songId, SeparationStatus.Processing);
            ProgressChanged?.Invoke(this, new SeparationProgressEventArgs(songId, SeparationStatus.Processing, 0));

            tempWavPath = Path.Combine(AppPaths.TempRoot, $"{songId}-{Guid.NewGuid():N}.wav");
            TranscodeToWav(song.FilePath, tempWavPath);

            var outputRoot = AppPaths.SeparatedFolderFor(songId);
            var progress = new Progress<int>(percent =>
                ProgressChanged?.Invoke(this, new SeparationProgressEventArgs(songId, SeparationStatus.Processing, percent)));

            var logPath = Path.Combine(outputRoot, "separation.log");
            await using var logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
            var logLine = new Progress<string>(line => logWriter.WriteLine(line));

            var result = await _demucsRunner.RunAsync(tempWavPath, outputRoot, progress, logLine, cancellationToken);

            var finalInstrumental = Path.Combine(outputRoot, "instrumental.wav");
            var finalVocals = Path.Combine(outputRoot, "vocals.wav");
            File.Copy(result.InstrumentalPath, finalInstrumental, overwrite: true);
            File.Copy(result.VocalsPath, finalVocals, overwrite: true);

            var demucsModelDir = Path.Combine(outputRoot, DemucsRunner.ModelName);
            if (Directory.Exists(demucsModelDir))
            {
                Directory.Delete(demucsModelDir, recursive: true);
            }

            await _songRepository.UpdateSeparationStatusAsync(songId, SeparationStatus.Ready, finalInstrumental, finalVocals);
            ProgressChanged?.Invoke(this, new SeparationProgressEventArgs(songId, SeparationStatus.Ready, 100));
        }
        catch (Exception ex)
        {
            await _songRepository.UpdateSeparationStatusAsync(songId, SeparationStatus.Failed, error: ex.Message);
            ProgressChanged?.Invoke(this, new SeparationProgressEventArgs(songId, SeparationStatus.Failed, Message: ex.Message));
        }
        finally
        {
            if (tempWavPath is not null && File.Exists(tempWavPath))
            {
                File.Delete(tempWavPath);
            }
        }
    }

    private static void TranscodeToWav(string sourcePath, string destWavPath)
    {
        using var reader = new MediaFoundationReader(sourcePath);
        WaveFileWriter.CreateWaveFile16(destWavPath, reader.ToSampleProvider());
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();
        try
        {
            _workerTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // best-effort shutdown
        }

        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
