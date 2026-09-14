using KaraokeMaster.Core.Audio;
using NAudio.Wave;
using Xunit;

namespace KaraokeMaster.Core.Tests.Audio;

public class EchoEffectSampleProviderTests
{
    [Fact]
    public void Read_WhenDisabled_PassesThroughUnchanged()
    {
        var source = new FakeSampleProvider([0.1f, 0.2f, 0.3f, 0.4f]);
        var echo = new EchoEffectSampleProvider(source) { Enabled = false };

        var buffer = new float[4];
        var read = echo.Read(buffer, 0, 4);

        Assert.Equal(4, read);
        Assert.Equal([0.1f, 0.2f, 0.3f, 0.4f], buffer);
    }

    [Fact]
    public void Read_WhenEnabled_ProducesDelayedEchoAtExpectedOffset()
    {
        // 100 samples/sec mono makes the delay-in-samples easy to reason about.
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(100, 1);
        var impulseThenSilence = new float[20];
        impulseThenSilence[0] = 1.0f;
        var source = new FakeSampleProvider(impulseThenSilence, waveFormat);

        var echo = new EchoEffectSampleProvider(source)
        {
            Enabled = true,
            WetMix = 0.5f,
            FeedbackGain = 0f, // isolate a single repeat, no cascading feedback
            DelayTime = TimeSpan.FromSeconds(0.1), // 10 samples at 100Hz
        };

        var buffer = new float[20];
        var read = echo.Read(buffer, 0, 20);

        Assert.Equal(20, read);
        Assert.Equal(1.0f, buffer[0], 3);
        Assert.Equal(0.5f, buffer[10], 3); // the impulse echoes back 10 samples later, scaled by WetMix
        for (var i = 0; i < 20; i++)
        {
            if (i is 0 or 10)
            {
                continue;
            }
            Assert.Equal(0f, buffer[i], 3);
        }
    }

    [Fact]
    public void Read_WithFeedback_ProducesDecayingRepeats()
    {
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(100, 1);
        var impulseThenSilence = new float[30];
        impulseThenSilence[0] = 1.0f;
        var source = new FakeSampleProvider(impulseThenSilence, waveFormat);

        var echo = new EchoEffectSampleProvider(source)
        {
            Enabled = true,
            WetMix = 1.0f,
            FeedbackGain = 0.5f,
            DelayTime = TimeSpan.FromSeconds(0.1),
        };

        var buffer = new float[30];
        echo.Read(buffer, 0, 30);

        Assert.Equal(1.0f, buffer[0], 3);
        Assert.Equal(1.0f, buffer[10], 3); // first repeat: delayed 1.0 * WetMix(1.0)
        Assert.Equal(0.5f, buffer[20], 3); // second repeat: decayed by FeedbackGain(0.5) then * WetMix(1.0)
    }

    [Fact]
    public void DelayTime_ChangedMidStream_ResizesBufferWithoutThrowing()
    {
        var source = new FakeSampleProvider(new float[100]);
        var echo = new EchoEffectSampleProvider(source) { Enabled = true };

        var buffer = new float[50];
        echo.Read(buffer, 0, 50);

        echo.DelayTime = TimeSpan.FromMilliseconds(50);
        var exception = Record.Exception(() => echo.Read(buffer, 0, 50));

        Assert.Null(exception);
    }
}
