using KaraokeMaster.Core.Audio;
using KaraokeMaster.Core.Models;
using KaraokeMaster.Core.Settings;

namespace KaraokeMaster.App.Controls;

public sealed class NowPlayingControl : UserControl
{
    private const int RowWidth = 300;

    private readonly AudioEngine _audioEngine;

    private readonly FlowLayoutPanel _mainPanel = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(4),
    };

    private readonly Label _nowPlayingLabel = new()
    {
        Width = RowWidth,
        Height = 24,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "No song loaded",
        Font = new Font(Control.DefaultFont, FontStyle.Bold),
    };

    private readonly TrackBar _seekBar = new() { Width = RowWidth, Minimum = 0, Maximum = 1000, TickStyle = TickStyle.None };
    private readonly Label _timeLabel = new() { Width = RowWidth, Height = 20, TextAlign = ContentAlignment.MiddleRight, Text = "0:00 / 0:00" };
    private readonly Button _playPauseButton = new() { Text = "Play", Width = 80 };
    private readonly Button _stopButton = new() { Text = "Stop", Width = 80 };
    private readonly TrackBar _volumeBar = new() { Minimum = 0, Maximum = 100, Value = 100, Width = 110, TickStyle = TickStyle.None };

    private readonly TrackBar _pitchBar = new() { Minimum = -12, Maximum = 12, Value = 0, Width = 150, TickFrequency = 1, TickStyle = TickStyle.BottomRight };
    private readonly Label _pitchValueLabel = new() { Text = "0 st", AutoSize = true, Padding = new Padding(6, 8, 0, 0) };
    private readonly TrackBar _tempoBar = new() { Minimum = -30, Maximum = 30, Value = 0, Width = 150, TickFrequency = 5, TickStyle = TickStyle.BottomRight };
    private readonly Label _tempoValueLabel = new() { Text = "0%", AutoSize = true, Padding = new Padding(6, 8, 0, 0) };

    private readonly ComboBox _outputDeviceCombo = new() { Width = RowWidth, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _micDeviceCombo = new() { Width = RowWidth, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _micToggleButton = new() { Text = "Mic: Off", Width = 100 };
    private readonly TrackBar _micGainBar = new() { Minimum = 0, Maximum = 200, Value = 100, Width = 150, TickStyle = TickStyle.None };
    private readonly Label _micGainValueLabel = new() { Text = "100%", AutoSize = true, Padding = new Padding(6, 8, 0, 0) };

    private readonly CheckBox _echoEnabledCheckBox = new() { Text = "Echo", Width = 60 };
    private readonly TrackBar _echoWetBar = new() { Minimum = 0, Maximum = 100, Value = 30, Width = 110, TickStyle = TickStyle.None };
    private readonly TrackBar _echoFeedbackBar = new() { Minimum = 0, Maximum = 90, Value = 35, Width = 110, TickStyle = TickStyle.None };
    private readonly TrackBar _echoDelayBar = new() { Minimum = 50, Maximum = 800, Value = 300, Width = 110, TickStyle = TickStyle.None };

    private bool _isSeeking;
    private Song? _currentSong;

    public Song? CurrentSong => _currentSong;

    public NowPlayingControl(AudioEngine audioEngine)
    {
        _audioEngine = audioEngine;

        var transportPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        transportPanel.Controls.Add(_playPauseButton);
        transportPanel.Controls.Add(_stopButton);
        transportPanel.Controls.Add(new Label { Text = "Vol", AutoSize = true, Padding = new Padding(12, 8, 4, 0) });
        transportPanel.Controls.Add(_volumeBar);

        var pitchPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        pitchPanel.Controls.Add(new Label { Text = "Key", AutoSize = true, Padding = new Padding(0, 8, 4, 0) });
        pitchPanel.Controls.Add(_pitchBar);
        pitchPanel.Controls.Add(_pitchValueLabel);

        var tempoPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        tempoPanel.Controls.Add(new Label { Text = "Tempo", AutoSize = true, Padding = new Padding(0, 8, 4, 0) });
        tempoPanel.Controls.Add(_tempoBar);
        tempoPanel.Controls.Add(_tempoValueLabel);

        var micHeaderPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        micHeaderPanel.Controls.Add(new Label { Text = "Mic:", AutoSize = true, Padding = new Padding(0, 8, 4, 0) });
        micHeaderPanel.Controls.Add(_micToggleButton);
        micHeaderPanel.Controls.Add(new Label { Text = "Gain", AutoSize = true, Padding = new Padding(12, 8, 4, 0) });
        micHeaderPanel.Controls.Add(_micGainBar);
        micHeaderPanel.Controls.Add(_micGainValueLabel);

        var echoPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        echoPanel.Controls.Add(_echoEnabledCheckBox);
        echoPanel.Controls.Add(new Label { Text = "Wet", AutoSize = true, Padding = new Padding(8, 8, 4, 0) });
        echoPanel.Controls.Add(_echoWetBar);
        echoPanel.Controls.Add(new Label { Text = "Fdbk", AutoSize = true, Padding = new Padding(8, 8, 4, 0) });
        echoPanel.Controls.Add(_echoFeedbackBar);
        echoPanel.Controls.Add(new Label { Text = "Delay", AutoSize = true, Padding = new Padding(8, 8, 4, 0) });
        echoPanel.Controls.Add(_echoDelayBar);

        _mainPanel.Controls.Add(_nowPlayingLabel);
        _mainPanel.Controls.Add(_seekBar);
        _mainPanel.Controls.Add(_timeLabel);
        _mainPanel.Controls.Add(transportPanel);
        _mainPanel.Controls.Add(pitchPanel);
        _mainPanel.Controls.Add(tempoPanel);
        _mainPanel.Controls.Add(new Label { Text = "Output device:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        _mainPanel.Controls.Add(_outputDeviceCombo);
        _mainPanel.Controls.Add(new Label { Text = "Mic input device:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        _mainPanel.Controls.Add(_micDeviceCombo);
        _mainPanel.Controls.Add(micHeaderPanel);
        _mainPanel.Controls.Add(echoPanel);

        Controls.Add(_mainPanel);

        LoadOutputDevices();
        LoadInputDevices();

        _playPauseButton.Click += (_, _) => TogglePlayPause();
        _stopButton.Click += (_, _) => _audioEngine.Stop();
        _volumeBar.Scroll += (_, _) => _audioEngine.Volume = _volumeBar.Value / 100f;
        _outputDeviceCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_outputDeviceCombo.SelectedItem is DeviceItem item)
            {
                _audioEngine.SetOutputDevice(item.Id);
            }
        };
        _micDeviceCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_micDeviceCombo.SelectedItem is DeviceItem item)
            {
                _audioEngine.SetMicDevice(item.Id);
            }
        };

        _pitchBar.Scroll += (_, _) =>
        {
            _audioEngine.PitchSemitones = _pitchBar.Value;
            _pitchValueLabel.Text = $"{_pitchBar.Value:+#;-#;0} st";
        };
        _tempoBar.Scroll += (_, _) =>
        {
            _audioEngine.TempoChangePercent = _tempoBar.Value;
            _tempoValueLabel.Text = $"{_tempoBar.Value:+#;-#;0}%";
        };

        _micToggleButton.Click += (_, _) => ToggleMic();
        _micGainBar.Scroll += (_, _) =>
        {
            _audioEngine.MicGain = _micGainBar.Value / 100f;
            _micGainValueLabel.Text = $"{_micGainBar.Value}%";
        };
        _echoEnabledCheckBox.CheckedChanged += (_, _) => _audioEngine.EchoEnabled = _echoEnabledCheckBox.Checked;
        _echoWetBar.Scroll += (_, _) => _audioEngine.EchoWetMix = _echoWetBar.Value / 100f;
        _echoFeedbackBar.Scroll += (_, _) => _audioEngine.EchoFeedback = _echoFeedbackBar.Value / 100f;
        _echoDelayBar.Scroll += (_, _) => _audioEngine.EchoDelay = TimeSpan.FromMilliseconds(_echoDelayBar.Value);

        _seekBar.MouseDown += (_, _) => _isSeeking = true;
        _seekBar.MouseUp += (_, _) =>
        {
            _isSeeking = false;
            if (_audioEngine.Duration > TimeSpan.Zero)
            {
                var target = TimeSpan.FromSeconds(_audioEngine.Duration.TotalSeconds * _seekBar.Value / 1000.0);
                _audioEngine.Seek(target);
            }
        };

        _audioEngine.PositionChanged += (_, position) =>
        {
            if (IsDisposed)
            {
                return;
            }
            BeginInvoke(new Action(() => UpdatePosition(position)));
        };
        _audioEngine.PlaybackStopped += (_, _) =>
        {
            if (IsDisposed)
            {
                return;
            }
            BeginInvoke(new Action(() => _playPauseButton.Text = "Play"));
        };
    }

    public void ApplySettings(AppSettings settings)
    {
        _volumeBar.Value = Math.Clamp((int)(settings.MasterVolume * 100), _volumeBar.Minimum, _volumeBar.Maximum);
        _audioEngine.Volume = settings.MasterVolume;

        _micGainBar.Value = Math.Clamp((int)(settings.MicGain * 100), _micGainBar.Minimum, _micGainBar.Maximum);
        _audioEngine.MicGain = settings.MicGain;
        _micGainValueLabel.Text = $"{_micGainBar.Value}%";

        _pitchBar.Value = Math.Clamp((int)settings.PitchSemitones, _pitchBar.Minimum, _pitchBar.Maximum);
        _audioEngine.PitchSemitones = _pitchBar.Value;
        _pitchValueLabel.Text = $"{_pitchBar.Value:+#;-#;0} st";

        _tempoBar.Value = Math.Clamp((int)settings.TempoChangePercent, _tempoBar.Minimum, _tempoBar.Maximum);
        _audioEngine.TempoChangePercent = _tempoBar.Value;
        _tempoValueLabel.Text = $"{_tempoBar.Value:+#;-#;0}%";

        _echoEnabledCheckBox.Checked = settings.EchoEnabled;
        _audioEngine.EchoEnabled = settings.EchoEnabled;

        _echoWetBar.Value = Math.Clamp((int)(settings.EchoWetMix * 100), _echoWetBar.Minimum, _echoWetBar.Maximum);
        _audioEngine.EchoWetMix = settings.EchoWetMix;

        _echoFeedbackBar.Value = Math.Clamp((int)(settings.EchoFeedback * 100), _echoFeedbackBar.Minimum, _echoFeedbackBar.Maximum);
        _audioEngine.EchoFeedback = settings.EchoFeedback;

        _echoDelayBar.Value = Math.Clamp(settings.EchoDelayMs, _echoDelayBar.Minimum, _echoDelayBar.Maximum);
        _audioEngine.EchoDelay = TimeSpan.FromMilliseconds(settings.EchoDelayMs);

        if (settings.OutputDeviceId is not null && SelectDeviceInCombo(_outputDeviceCombo, settings.OutputDeviceId))
        {
            _audioEngine.SetOutputDevice(settings.OutputDeviceId);
        }

        if (settings.MicDeviceId is not null && SelectDeviceInCombo(_micDeviceCombo, settings.MicDeviceId))
        {
            _audioEngine.SetMicDevice(settings.MicDeviceId);
        }
    }

    public void CaptureSettings(AppSettings settings)
    {
        settings.MasterVolume = _volumeBar.Value / 100f;
        settings.MicGain = _micGainBar.Value / 100f;
        settings.PitchSemitones = _pitchBar.Value;
        settings.TempoChangePercent = _tempoBar.Value;
        settings.EchoEnabled = _echoEnabledCheckBox.Checked;
        settings.EchoWetMix = _echoWetBar.Value / 100f;
        settings.EchoFeedback = _echoFeedbackBar.Value / 100f;
        settings.EchoDelayMs = _echoDelayBar.Value;
        settings.OutputDeviceId = (_outputDeviceCombo.SelectedItem as DeviceItem)?.Id;
        settings.MicDeviceId = (_micDeviceCombo.SelectedItem as DeviceItem)?.Id;
    }

    private static bool SelectDeviceInCombo(ComboBox combo, string deviceId)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is DeviceItem item && item.Id == deviceId)
            {
                combo.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }

    private void LoadOutputDevices()
    {
        _outputDeviceCombo.Items.Clear();
        foreach (var device in _audioEngine.GetOutputDevices())
        {
            _outputDeviceCombo.Items.Add(new DeviceItem(device.Id, device.Name));
        }

        if (_outputDeviceCombo.Items.Count > 0)
        {
            _outputDeviceCombo.SelectedIndex = 0;
        }
    }

    private void LoadInputDevices()
    {
        _micDeviceCombo.Items.Clear();
        foreach (var device in _audioEngine.GetInputDevices())
        {
            _micDeviceCombo.Items.Add(new DeviceItem(device.Id, device.Name));
        }

        if (_micDeviceCombo.Items.Count > 0)
        {
            _micDeviceCombo.SelectedIndex = 0;
        }
    }

    private void ToggleMic()
    {
        if (_audioEngine.IsMicActive)
        {
            _audioEngine.StopMic();
            _micToggleButton.Text = "Mic: Off";
        }
        else
        {
            _audioEngine.StartMic();
            _micToggleButton.Text = "Mic: On";
        }
    }

    public void LoadAndPlay(Song song)
    {
        _currentSong = song;

        var usingInstrumental = song.SeparationStatus == SeparationStatus.Ready && song.InstrumentalPath is not null;
        var pathToPlay = usingInstrumental ? song.InstrumentalPath! : song.FilePath;

        _nowPlayingLabel.Text = usingInstrumental
            ? $"{song.Title} — {song.Artist ?? "Unknown Artist"} (instrumental)"
            : $"{song.Title} — {song.Artist ?? "Unknown Artist"}";

        _audioEngine.Load(pathToPlay);
        _audioEngine.Play();
        _playPauseButton.Text = "Pause";
    }

    private void TogglePlayPause()
    {
        if (_currentSong is null)
        {
            return;
        }

        if (_audioEngine.State == PlaybackState.Playing)
        {
            _audioEngine.Pause();
            _playPauseButton.Text = "Play";
        }
        else
        {
            _audioEngine.Play();
            _playPauseButton.Text = "Pause";
        }
    }

    private void UpdatePosition(TimeSpan position)
    {
        if (!_isSeeking && _audioEngine.Duration > TimeSpan.Zero)
        {
            var ratio = Math.Clamp(position.TotalSeconds / _audioEngine.Duration.TotalSeconds, 0, 1);
            _seekBar.Value = (int)(ratio * 1000);
        }

        _timeLabel.Text = $"{Format(position)} / {Format(_audioEngine.Duration)}";
    }

    private static string Format(TimeSpan time) =>
        time.ToString(time.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss");

    private sealed record DeviceItem(string Id, string Name)
    {
        public override string ToString() => Name;
    }
}
