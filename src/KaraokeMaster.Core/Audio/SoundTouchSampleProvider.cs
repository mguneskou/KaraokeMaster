using NAudio.Wave;
using SoundTouch;

namespace KaraokeMaster.Core.Audio;

/// <summary>
/// Applies independent pitch (key) and tempo shifting to a float sample stream using the
/// SoundTouch DSP algorithm.
/// </summary>
/// <remarks>
/// This wraps the base <c>SoundTouch.Net</c> package's <see cref="SoundTouchProcessor"/> directly
/// rather than using <c>SoundTouch.Net.NAudioSupport</c>: that support package's published 2.3.2
/// build hard-references the old monolithic NAudio 1.9.0 assembly, whose <c>NAudio.dll</c> no
/// longer exists in the same shape under modern split-package NAudio 2.x (the type now lives in
/// NAudio.Core.dll), so it throws a TypeLoadException at runtime. This class ports the same
/// read/flush logic the support package uses, against our own ISampleProvider pipeline instead.
/// </remarks>
public sealed class SoundTouchSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly SoundTouchProcessor _processor;
    private readonly float[] _sourceBuffer;
    private readonly object _syncLock = new();
    private bool _isFlushed;

    public SoundTouchSampleProvider(ISampleProvider source)
    {
        _source = source;
        _processor = new SoundTouchProcessor
        {
            SampleRate = source.WaveFormat.SampleRate,
            Channels = source.WaveFormat.Channels,
        };
        _processor.Tempo = 1.0;
        _processor.Pitch = 1.0;
        _processor.Rate = 1.0;

        // Small, fixed-size chunks (matching the scale of SoundTouch.Net.NAudioSupport's own
        // reference implementation) — sizing this to a large multi-second buffer previously
        // caused the underlying file reader to be pulled in huge bursts instead of smooth
        // continuous reads, which made playback position jump in ~1-second steps.
        _sourceBuffer = new float[4096];
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>Pitch shift in semitones, roughly -12..+12 for a musically useful range.</summary>
    public double PitchSemiTones
    {
        get => _processor.PitchSemiTones;
        set
        {
            lock (_syncLock)
            {
                _processor.PitchSemiTones = value;
            }
        }
    }

    /// <summary>Tempo change as a percentage of original speed (0 = unchanged).</summary>
    public double TempoChangePercent
    {
        get => _processor.TempoChange;
        set
        {
            lock (_syncLock)
            {
                _processor.TempoChange = value;
            }
        }
    }

    /// <summary>Drops any buffered samples, e.g. after a seek, so stale audio isn't replayed.</summary>
    public void Reset()
    {
        lock (_syncLock)
        {
            _processor.Clear();
            _isFlushed = false;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var channels = WaveFormat.Channels;

        lock (_syncLock)
        {
            var samplesRequiredPerChannel = count / channels;

            while (_processor.AvailableSamples < samplesRequiredPerChannel)
            {
                var samplesRead = _source.Read(_sourceBuffer, 0, _sourceBuffer.Length);
                if (samplesRead == 0)
                {
                    if (!_isFlushed)
                    {
                        _isFlushed = true;
                        _processor.Flush();
                    }
                    break;
                }

                _processor.PutSamples(_sourceBuffer.AsSpan(0, samplesRead), samplesRead / channels);
            }

            var output = buffer.AsSpan(offset, count);
            output.Clear();
            var framesReceived = _processor.ReceiveSamples(output, output.Length / channels);
            return framesReceived * channels;
        }
    }
}
