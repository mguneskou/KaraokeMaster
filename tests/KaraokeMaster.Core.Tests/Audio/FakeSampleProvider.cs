using NAudio.Wave;

namespace KaraokeMaster.Core.Tests.Audio;

internal sealed class FakeSampleProvider(float[] data, WaveFormat? waveFormat = null) : ISampleProvider
{
    private int _position;

    public WaveFormat WaveFormat { get; } = waveFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

    public int Read(float[] buffer, int offset, int count)
    {
        var available = Math.Min(count, data.Length - _position);
        if (available <= 0)
        {
            return 0;
        }

        Array.Copy(data, _position, buffer, offset, available);
        _position += available;
        return available;
    }
}
