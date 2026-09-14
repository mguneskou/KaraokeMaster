using NAudio.Wave;

namespace KaraokeMaster.Core.Audio;

/// <summary>
/// A simple feedback delay line — the classic karaoke-machine mic echo effect. Operates on the
/// interleaved float sample stream directly, so the delay buffer length is expressed in samples
/// (SampleRate * Channels * seconds) rather than needing per-channel bookkeeping.
/// </summary>
public sealed class EchoEffectSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float[] _delayBuffer = [];
    private int _writeIndex;
    private TimeSpan _delayTime = TimeSpan.FromMilliseconds(300);

    public EchoEffectSampleProvider(ISampleProvider source)
    {
        _source = source;
        RebuildDelayBuffer();
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public bool Enabled { get; set; }

    /// <summary>How much of the delayed signal feeds back into itself (0..~0.9 before it gets unstable).</summary>
    public float FeedbackGain { get; set; } = 0.35f;

    /// <summary>How loud the echoed repeats are relative to the dry signal (0..1).</summary>
    public float WetMix { get; set; } = 0.3f;

    public TimeSpan DelayTime
    {
        get => _delayTime;
        set
        {
            _delayTime = value;
            RebuildDelayBuffer();
        }
    }

    private void RebuildDelayBuffer()
    {
        var samples = (int)(WaveFormat.SampleRate * WaveFormat.Channels * _delayTime.TotalSeconds);
        samples = Math.Max(samples, WaveFormat.Channels);
        _delayBuffer = new float[samples];
        _writeIndex = 0;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samplesRead = _source.Read(buffer, offset, count);

        if (!Enabled || _delayBuffer.Length == 0)
        {
            return samplesRead;
        }

        for (var i = 0; i < samplesRead; i++)
        {
            var idx = offset + i;
            var delayed = _delayBuffer[_writeIndex];
            var input = buffer[idx];

            buffer[idx] = input + delayed * WetMix;
            _delayBuffer[_writeIndex] = input + delayed * FeedbackGain;
            _writeIndex = (_writeIndex + 1) % _delayBuffer.Length;
        }

        return samplesRead;
    }
}
