using KaraokeMaster.Core.Settings;
using Xunit;

namespace KaraokeMaster.Core.Tests.Settings;

public class SettingsServiceTests : IDisposable
{
    private readonly string _path;

    public SettingsServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"karaoke-settings-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var service = new SettingsService(_path);

        var settings = service.Load();

        Assert.Equal(1f, settings.MasterVolume);
        Assert.Null(settings.OutputDeviceId);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var service = new SettingsService(_path);
        var original = new AppSettings
        {
            OutputDeviceId = "device-123",
            MicDeviceId = "mic-456",
            MasterVolume = 0.75f,
            MicGain = 1.5f,
            EchoEnabled = true,
            EchoWetMix = 0.4f,
            EchoFeedback = 0.5f,
            EchoDelayMs = 250,
            PitchSemitones = 3,
            TempoChangePercent = -10,
            WindowX = 100,
            WindowY = 200,
            WindowWidth = 1024,
            WindowHeight = 768,
        };

        service.Save(original);
        var loaded = service.Load();

        Assert.Equal(original.OutputDeviceId, loaded.OutputDeviceId);
        Assert.Equal(original.MicDeviceId, loaded.MicDeviceId);
        Assert.Equal(original.MasterVolume, loaded.MasterVolume);
        Assert.Equal(original.MicGain, loaded.MicGain);
        Assert.Equal(original.EchoEnabled, loaded.EchoEnabled);
        Assert.Equal(original.EchoWetMix, loaded.EchoWetMix);
        Assert.Equal(original.EchoFeedback, loaded.EchoFeedback);
        Assert.Equal(original.EchoDelayMs, loaded.EchoDelayMs);
        Assert.Equal(original.PitchSemitones, loaded.PitchSemitones);
        Assert.Equal(original.TempoChangePercent, loaded.TempoChangePercent);
        Assert.Equal(original.WindowX, loaded.WindowX);
        Assert.Equal(original.WindowY, loaded.WindowY);
        Assert.Equal(original.WindowWidth, loaded.WindowWidth);
        Assert.Equal(original.WindowHeight, loaded.WindowHeight);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaultsWithoutThrowing()
    {
        File.WriteAllText(_path, "{ not valid json ][");
        var service = new SettingsService(_path);

        var settings = service.Load();

        Assert.Equal(1f, settings.MasterVolume);
    }

    [Fact]
    public void Save_CreatesParentDirectoryIfMissing()
    {
        var nestedPath = Path.Combine(Path.GetTempPath(), $"karaoke-settings-dir-{Guid.NewGuid():N}", "settings.json");
        var service = new SettingsService(nestedPath);

        service.Save(new AppSettings());

        Assert.True(File.Exists(nestedPath));
        Directory.Delete(Path.GetDirectoryName(nestedPath)!, recursive: true);
    }
}
