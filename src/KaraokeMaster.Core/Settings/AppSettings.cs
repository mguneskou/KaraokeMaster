namespace KaraokeMaster.Core.Settings;

/// <summary>
/// Persisted user preferences — device selection, mixer levels, and window placement — so the
/// app doesn't reset to defaults every launch. Stored as JSON in data\settings.json, not the DB.
/// </summary>
public sealed class AppSettings
{
    public string? OutputDeviceId { get; set; }
    public string? MicDeviceId { get; set; }

    public float MasterVolume { get; set; } = 1f;
    public float MicGain { get; set; } = 1f;

    public bool EchoEnabled { get; set; }
    public float EchoWetMix { get; set; } = 0.3f;
    public float EchoFeedback { get; set; } = 0.35f;
    public int EchoDelayMs { get; set; } = 300;

    public double PitchSemitones { get; set; }
    public double TempoChangePercent { get; set; }

    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }
}
