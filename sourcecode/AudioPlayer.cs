using NAudio.Wave;

namespace DEP3;

sealed class AudioPlayer : IDisposable
{
    private WaveOutEvent? _output;
    private AudioFileReader? _reader;
    private EqualizerSampleProvider? _eq;
    private bool _disposed;
    private bool _ignoreStopEvent;
    private int _deviceNumber = -1;
    private string _eqPreset = "Flat";
    private float _volume = 0.7f;
    private string? _currentPath;

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;
    public bool HasTrack => _reader is not null;
    public TimeSpan CurrentTime => _reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan TotalTime => _reader?.TotalTime ?? TimeSpan.Zero;

    public event EventHandler? PlaybackStopped;

    public static IReadOnlyList<(int Number, string Name)> GetDevices()
    {
        var list = new List<(int, string)> { (-1, "System default") };
        for (var i = 0; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            list.Add((i, caps.ProductName));
        }

        return list;
    }

    public void Configure(int deviceNumber, string eqPreset, float volume)
    {
        _deviceNumber = deviceNumber;
        _eqPreset = eqPreset;
        _volume = Math.Clamp(volume, 0f, 1f);

        if (_reader is null || _currentPath is null)
            return;

        var position = _reader.CurrentTime;
        var playing = IsPlaying;
        Load(_currentPath, _volume, resumeAt: position, autoPlay: playing);
    }

    public void SetEqualizerPreset(string preset)
    {
        _eqPreset = preset;
        _eq?.ApplyPreset(preset);
    }

    public void Load(string path, float volume, TimeSpan? resumeAt = null, bool autoPlay = false)
    {
        StopInternal();

        _currentPath = path;
        _volume = Math.Clamp(volume, 0f, 1f);
        _reader = new AudioFileReader(path) { Volume = _volume };
        _eq = new EqualizerSampleProvider(_reader);
        _eq.ApplyPreset(_eqPreset);

        _output = new WaveOutEvent
        {
            DesiredLatency = 200,
            DeviceNumber = _deviceNumber
        };
        _output.Init(_eq);
        _output.PlaybackStopped += OnPlaybackStopped;

        if (resumeAt is not null)
            _reader.CurrentTime = resumeAt.Value;

        if (autoPlay)
            _output.Play();
    }

    public void Play() => _output?.Play();

    public void Pause()
    {
        if (_output is null)
            return;

        _ignoreStopEvent = true;
        _output.Pause();
    }

    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0f, 1f);
        if (_reader is not null)
            _reader.Volume = _volume;
    }

    public void Seek(double ratio)
    {
        if (_reader is null)
            return;

        ratio = Math.Clamp(ratio, 0.0, 1.0);
        _reader.CurrentTime = TimeSpan.FromMilliseconds(_reader.TotalTime.TotalMilliseconds * ratio);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_ignoreStopEvent)
        {
            _ignoreStopEvent = false;
            return;
        }

        if (e.Exception is null)
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    private void StopInternal()
    {
        if (_output is not null)
        {
            _ignoreStopEvent = true;
            _output.PlaybackStopped -= OnPlaybackStopped;
            try { _output.Stop(); } catch { /* ignore */ }
            _output.Dispose();
            _output = null;
        }

        _eq = null;
        _reader?.Dispose();
        _reader = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopInternal();
        _disposed = true;
    }
}
