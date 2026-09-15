using NAudio.Dsp;
using NAudio.Wave;

namespace KaraokeMaster.Core.Audio;

/// <summary>
/// Passthrough ISampleProvider: forwards samples unchanged (zero effect on playback) while
/// accumulating them into an FFT window for a real-time spectrum, for the performer window's
/// equalizer visualization. Downmixes to mono for analysis regardless of the source's channel
/// count, since a single bar display doesn't need separate L/R spectra.
/// </summary>
public sealed class SpectrumAnalyzerSampleProvider : ISampleProvider
{
    private const int FftLength = 1024;
    private const int FftLog2 = 10; // log2(FftLength)

    private readonly ISampleProvider _source;
    private readonly Complex[] _fftBuffer = new Complex[FftLength];
    private int _fftBufferIndex;

    private float[] _latestSpectrum = new float[FftLength / 2];

    public SpectrumAnalyzerSampleProvider(ISampleProvider source)
    {
        _source = source;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// Magnitude per FFT bin (index 0 = DC) from the most recently completed analysis window.
    /// Thread-safe to call from a UI thread while <see cref="Read"/> runs on the audio thread -
    /// returns a snapshot reference, never a half-written array.
    /// </summary>
    public float[] GetLatestSpectrum() => Volatile.Read(ref _latestSpectrum);

    public int Read(float[] buffer, int offset, int count)
    {
        var samplesRead = _source.Read(buffer, offset, count);
        var channels = WaveFormat.Channels;

        for (var i = 0; i + channels <= samplesRead; i += channels)
        {
            var sum = 0f;
            for (var ch = 0; ch < channels; ch++)
            {
                sum += buffer[offset + i + ch];
            }

            var mono = sum / channels;
            var window = (float)FastFourierTransform.HammingWindow(_fftBufferIndex, FftLength);
            _fftBuffer[_fftBufferIndex].X = mono * window;
            _fftBuffer[_fftBufferIndex].Y = 0;
            _fftBufferIndex++;

            if (_fftBufferIndex >= FftLength)
            {
                _fftBufferIndex = 0;
                AnalyzeWindow();
            }
        }

        return samplesRead;
    }

    private void AnalyzeWindow()
    {
        // FFT() mutates in place; the accumulation buffer is reused for the next window
        // immediately after, so operate on a copy.
        var data = (Complex[])_fftBuffer.Clone();
        FastFourierTransform.FFT(true, FftLog2, data);

        var magnitudes = new float[FftLength / 2];
        for (var b = 0; b < magnitudes.Length; b++)
        {
            magnitudes[b] = MathF.Sqrt((data[b].X * data[b].X) + (data[b].Y * data[b].Y));
        }

        Volatile.Write(ref _latestSpectrum, magnitudes);
    }
}
