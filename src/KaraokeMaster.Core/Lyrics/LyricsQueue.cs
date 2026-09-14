using System.Threading.Channels;
using KaraokeMaster.Core.Data;
using KaraokeMaster.Core.Models;
using KaraokeMaster.Core.Separation;

namespace KaraokeMaster.Core.Lyrics;

public sealed record LyricsProgressEventArgs(int SongId, SeparationStatus Status, int? PercentComplete = null, string? Message = null);

/// <summary>
/// Background worker that transcribes one song's isolated vocals stem into a word-timed .lrc at
/// a time, mirroring <see cref="SeparationQueue"/>'s single-worker shape. Requires vocal
/// separation to already be <see cref="SeparationStatus.Ready"/> — there's no vocals track to
/// transcribe otherwise.
/// </summary>
public sealed class LyricsQueue : IDisposable
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    private readonly SongRepository _songRepository;
    private readonly LyricsRunner _lyricsRunner;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;

    public event EventHandler<LyricsProgressEventArgs>? ProgressChanged;

    public LyricsQueue(SongRepository songRepository, PyEnvSetup pyEnvSetup)
    {
        _songRepository = songRepository;
        _lyricsRunner = new LyricsRunner(pyEnvSetup);
        _workerTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
    }

    public void Enqueue(int songId)
    {
        _songRepository.UpdateLyricsStatusAsync(songId, SeparationStatus.Queued).GetAwaiter().GetResult();
        ProgressChanged?.Invoke(this, new LyricsProgressEventArgs(songId, SeparationStatus.Queued));
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

        try
        {
            if (song.SeparationStatus != SeparationStatus.Ready || song.VocalsPath is null)
            {
                throw new InvalidOperationException(
                    "Vocals aren't separated yet — run \"Separate Vocals\" on this song first.");
            }

            await _songRepository.UpdateLyricsStatusAsync(songId, SeparationStatus.Processing);
            ProgressChanged?.Invoke(this, new LyricsProgressEventArgs(songId, SeparationStatus.Processing, 0));

            var outputRoot = AppPaths.SeparatedFolderFor(songId);
            var progress = new Progress<int>(percent =>
                ProgressChanged?.Invoke(this, new LyricsProgressEventArgs(songId, SeparationStatus.Processing, percent)));

            var logPath = Path.Combine(outputRoot, "lyrics.log");
            await using var logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
            var logLine = new Progress<string>(line => logWriter.WriteLine(line));

            var lrcPath = Path.Combine(outputRoot, "lyrics.lrc");
            await _lyricsRunner.RunAsync(song.VocalsPath, lrcPath, progress, logLine, cancellationToken: cancellationToken);

            await _songRepository.UpdateLyricsStatusAsync(songId, SeparationStatus.Ready, lrcPath: lrcPath);
            ProgressChanged?.Invoke(this, new LyricsProgressEventArgs(songId, SeparationStatus.Ready, 100));
        }
        catch (Exception ex)
        {
            await _songRepository.UpdateLyricsStatusAsync(songId, SeparationStatus.Failed, error: ex.Message);
            ProgressChanged?.Invoke(this, new LyricsProgressEventArgs(songId, SeparationStatus.Failed, Message: ex.Message));
        }
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
