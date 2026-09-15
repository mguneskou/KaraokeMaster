using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace KaraokeMaster.Core.Audio;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused,
}

public sealed record AudioDeviceInfo(string Id, string Name);

/// <summary>
/// Live karaoke mixing engine: plays a track (with independent pitch/tempo via
/// <see cref="SoundTouchSampleProvider"/>) and, independently, can capture a microphone
/// (through an <see cref="EchoEffectSampleProvider"/> and gain stage) — both continuously mixed
/// together into a single WASAPI output. The output device and mixer are long-lived once first
/// started, since a real karaoke session keeps the mic hot across song changes rather than
/// tying it to any one track's play/pause state.
/// </summary>
public sealed class AudioEngine : IDisposable
{
    private const int PositionTimerIntervalMs = 100;

    private static readonly WaveFormat MixFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private readonly MixingSampleProvider _mixer;
    private readonly VolumeSampleProvider _masterVolumeProvider;
    private readonly SpectrumAnalyzerSampleProvider _spectrumTap;

    private AudioFileReader? _fileReader;
    private SoundTouchSampleProvider? _pitchTempoProvider;
    private ISampleProvider? _fileMixInput;
    private bool _fileInputActive;

    private WasapiCapture? _micCapture;
    private BufferedWaveProvider? _micBuffer;
    private VolumeSampleProvider? _micGainProvider;
    private EchoEffectSampleProvider? _echoProvider;
    private ISampleProvider? _micMixInput;
    private string? _selectedMicDeviceId;

    private WasapiOut? _outputDevice;
    private Timer? _positionTimer;
    private string? _selectedOutputDeviceId;

    private float _volume = 1f;
    private double _pitchSemitones;
    private double _tempoChangePercent;
    private float _micGain = 1f;
    private bool _echoEnabled;
    private float _echoWetMix = 0.3f;
    private float _echoFeedback = 0.35f;
    private TimeSpan _echoDelay = TimeSpan.FromMilliseconds(300);

    public AudioEngine()
    {
        _mixer = new MixingSampleProvider(MixFormat) { ReadFully = true };
        _mixer.MixerInputEnded += OnMixerInputEnded;
        _masterVolumeProvider = new VolumeSampleProvider(_mixer) { Volume = _volume };
        _spectrumTap = new SpectrumAnalyzerSampleProvider(_masterVolumeProvider);
    }

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public string? CurrentFilePath { get; private set; }
    public TimeSpan Position => _fileReader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _fileReader?.TotalTime ?? TimeSpan.Zero;
    public bool IsMicActive => _micCapture is not null;

    /// <summary>
    /// FFT magnitude per bin (512 bins, index 0 = DC) from the most recently completed analysis
    /// window of the final mixed output (track + mic, post master volume) - for the performer
    /// window's equalizer visualization. Safe to poll from the UI thread at any rate.
    /// </summary>
    public float[] GetLatestSpectrum() => _spectrumTap.GetLatestSpectrum();

    /// <summary>Master volume applied to the combined (track + mic) output.</summary>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            _masterVolumeProvider.Volume = _volume;
        }
    }

    /// <summary>Pitch shift in semitones (musically useful range is roughly -12..+12).</summary>
    public double PitchSemitones
    {
        get => _pitchSemitones;
        set
        {
            _pitchSemitones = value;
            if (_pitchTempoProvider is not null)
            {
                _pitchTempoProvider.PitchSemiTones = value;
            }
        }
    }

    /// <summary>Tempo change as a percentage of original speed (0 = unchanged).</summary>
    public double TempoChangePercent
    {
        get => _tempoChangePercent;
        set
        {
            _tempoChangePercent = value;
            if (_pitchTempoProvider is not null)
            {
                _pitchTempoProvider.TempoChangePercent = value;
            }
        }
    }

    public float MicGain
    {
        get => _micGain;
        set
        {
            _micGain = Math.Clamp(value, 0f, 2f);
            if (_micGainProvider is not null)
            {
                _micGainProvider.Volume = _micGain;
            }
        }
    }

    public bool EchoEnabled
    {
        get => _echoEnabled;
        set
        {
            _echoEnabled = value;
            if (_echoProvider is not null)
            {
                _echoProvider.Enabled = value;
            }
        }
    }

    /// <summary>How loud the echoed repeats are relative to the dry mic signal (0..1).</summary>
    public float EchoWetMix
    {
        get => _echoWetMix;
        set
        {
            _echoWetMix = Math.Clamp(value, 0f, 1f);
            if (_echoProvider is not null)
            {
                _echoProvider.WetMix = _echoWetMix;
            }
        }
    }

    /// <summary>How much each echo repeat feeds back into itself (0..~0.9 before it turns into runaway feedback).</summary>
    public float EchoFeedback
    {
        get => _echoFeedback;
        set
        {
            _echoFeedback = Math.Clamp(value, 0f, 0.9f);
            if (_echoProvider is not null)
            {
                _echoProvider.FeedbackGain = _echoFeedback;
            }
        }
    }

    public TimeSpan EchoDelay
    {
        get => _echoDelay;
        set
        {
            _echoDelay = value;
            if (_echoProvider is not null)
            {
                _echoProvider.DelayTime = value;
            }
        }
    }

    /// <summary>Fires when the loaded track halts, whether from Stop() or reaching end of track.</summary>
    public event EventHandler? PlaybackStopped;

    /// <summary>Fires roughly 10x/second while a track is loaded, for progress bars and future LRC sync.</summary>
    public event EventHandler<TimeSpan>? PositionChanged;

    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(d => new AudioDeviceInfo(d.ID, d.FriendlyName))
            .ToList();
    }

    public IReadOnlyList<AudioDeviceInfo> GetInputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => new AudioDeviceInfo(d.ID, d.FriendlyName))
            .ToList();
    }

    public void SetOutputDevice(string? deviceId) => _selectedOutputDeviceId = deviceId;

    public void SetMicDevice(string? deviceId) => _selectedMicDeviceId = deviceId;

    public void Load(string filePath)
    {
        DetachFileInput();
        _fileReader?.Dispose();

        _fileReader = new AudioFileReader(filePath);
        _pitchTempoProvider = new SoundTouchSampleProvider(_fileReader)
        {
            PitchSemiTones = _pitchSemitones,
            TempoChangePercent = _tempoChangePercent,
        };
        _fileMixInput = AudioFormatHelpers.ToMixFormat(_pitchTempoProvider, MixFormat);
        CurrentFilePath = filePath;
        State = PlaybackState.Stopped;
    }

    /// <summary>Starts playback, or resumes if currently paused.</summary>
    public void Play()
    {
        if (_fileMixInput is null)
        {
            return;
        }

        EnsureOutputStarted();
        AttachFileInput();
        State = PlaybackState.Playing;
    }

    public void Pause()
    {
        DetachFileInput();
        State = PlaybackState.Paused;
    }

    public void Stop()
    {
        DetachFileInput();

        if (_fileReader is not null)
        {
            _fileReader.CurrentTime = TimeSpan.Zero;
        }

        _pitchTempoProvider?.Reset();
        State = PlaybackState.Stopped;
    }

    public void Seek(TimeSpan position)
    {
        if (_fileReader is null)
        {
            return;
        }

        var clampedSeconds = Math.Clamp(position.TotalSeconds, 0, Math.Max(0, _fileReader.TotalTime.TotalSeconds));
        _fileReader.CurrentTime = TimeSpan.FromSeconds(clampedSeconds);
        _pitchTempoProvider?.Reset();
    }

    public void StartMic()
    {
        if (_micCapture is not null)
        {
            return;
        }

        using var enumerator = new MMDeviceEnumerator();
        var device = _selectedMicDeviceId is not null
            ? enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).FirstOrDefault(d => d.ID == _selectedMicDeviceId)
            : null;
        device ??= enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

        _micCapture = new WasapiCapture(device);
        _micBuffer = new BufferedWaveProvider(_micCapture.WaveFormat)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromSeconds(2),
        };
        _micCapture.DataAvailable += OnMicDataAvailable;
        _micCapture.StartRecording();

        var micSampleProvider = AudioFormatHelpers.ToMixFormat(_micBuffer.ToSampleProvider(), MixFormat);
        _micGainProvider = new VolumeSampleProvider(micSampleProvider) { Volume = _micGain };
        _echoProvider = new EchoEffectSampleProvider(_micGainProvider)
        {
            Enabled = _echoEnabled,
            WetMix = _echoWetMix,
            FeedbackGain = _echoFeedback,
            DelayTime = _echoDelay,
        };
        _micMixInput = _echoProvider;

        EnsureOutputStarted();
        _mixer.AddMixerInput(_micMixInput);
    }

    public void StopMic()
    {
        if (_micCapture is null)
        {
            return;
        }

        if (_micMixInput is not null)
        {
            _mixer.RemoveMixerInput(_micMixInput);
            _micMixInput = null;
        }

        _micCapture.DataAvailable -= OnMicDataAvailable;
        _micCapture.StopRecording();
        _micCapture.Dispose();
        _micCapture = null;
        _micBuffer = null;
        _micGainProvider = null;
        _echoProvider = null;
    }

    private void OnMicDataAvailable(object? sender, WaveInEventArgs e)
    {
        _micBuffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

    private void AttachFileInput()
    {
        if (_fileMixInput is not null && !_fileInputActive)
        {
            _mixer.AddMixerInput(_fileMixInput);
            _fileInputActive = true;
        }
    }

    private void DetachFileInput()
    {
        if (_fileMixInput is not null && _fileInputActive)
        {
            _mixer.RemoveMixerInput(_fileMixInput);
            _fileInputActive = false;
        }
    }

    private void OnMixerInputEnded(object? sender, SampleProviderEventArgs e)
    {
        if (!ReferenceEquals(e.SampleProvider, _fileMixInput))
        {
            return;
        }

        _fileInputActive = false;
        State = PlaybackState.Stopped;
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureOutputStarted()
    {
        if (_outputDevice is not null)
        {
            return;
        }

        _outputDevice = CreateOutputDevice();
        _outputDevice.Init(new SampleToWaveProvider(_spectrumTap));
        _outputDevice.Play();
        StartPositionTimer();
    }

    private WasapiOut CreateOutputDevice()
    {
        using var enumerator = new MMDeviceEnumerator();
        var device = _selectedOutputDeviceId is not null
            ? enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .FirstOrDefault(d => d.ID == _selectedOutputDeviceId)
            : null;
        device ??= enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        // Explicit device + event-sync mode: the parameterless/default-device constructor path
        // falls back to timer-polled rendering, which added a very noticeable resume delay when
        // a mixer input was re-attached after being paused.
        return new WasapiOut(device, AudioClientShareMode.Shared, true, 100);
    }

    private void StartPositionTimer()
    {
        _positionTimer = new Timer(_ => PositionChanged?.Invoke(this, Position), null, 0, PositionTimerIntervalMs);
    }

    private void StopPositionTimer()
    {
        _positionTimer?.Dispose();
        _positionTimer = null;
    }

    public void Dispose()
    {
        StopMic();
        Stop();

        if (_outputDevice is not null)
        {
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
        }

        StopPositionTimer();
        _fileReader?.Dispose();
        GC.SuppressFinalize(this);
    }
}
