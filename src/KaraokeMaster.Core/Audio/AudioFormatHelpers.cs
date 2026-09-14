using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace KaraokeMaster.Core.Audio;

internal static class AudioFormatHelpers
{
    /// <summary>
    /// Adapts a sample provider to a target sample rate/channel count so it can be fed into a
    /// <see cref="MixingSampleProvider"/>, which requires every input to share an identical format.
    /// Handles the practical cases (mono mic into a stereo mix, differing device sample rates);
    /// anything beyond mono/stereo is passed through unchanged since consumer audio never uses it.
    /// </summary>
    public static ISampleProvider ToMixFormat(ISampleProvider source, WaveFormat targetFormat)
    {
        var result = source;

        if (result.WaveFormat.SampleRate != targetFormat.SampleRate)
        {
            result = new WdlResamplingSampleProvider(result, targetFormat.SampleRate);
        }

        if (result.WaveFormat.Channels != targetFormat.Channels)
        {
            result = (result.WaveFormat.Channels, targetFormat.Channels) switch
            {
                (1, 2) => new MonoToStereoSampleProvider(result),
                (2, 1) => new StereoToMonoSampleProvider(result),
                _ => result,
            };
        }

        return result;
    }
}
