using NAudio.Dsp;
using NAudio.Wave;

namespace DEP3;

sealed class EqualizerSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private BiQuadFilter[][] _filters;
    private readonly float[] _gainsDb;
    private readonly object _lock = new();

    public EqualizerSampleProvider(ISampleProvider source)
    {
        _source = source;
        _gainsDb = new float[EqPresets.Frequencies.Length];
        _filters = CreateFilters();
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public void ApplyPreset(string preset)
    {
        var gains = EqPresets.GetGains(preset);
        lock (_lock)
        {
            Array.Copy(gains, _gainsDb, _gainsDb.Length);
            _filters = CreateFilters();
        }
    }

    private BiQuadFilter[][] CreateFilters()
    {
        var channels = Math.Max(1, _source.WaveFormat.Channels);
        var rate = _source.WaveFormat.SampleRate;
        var bands = new BiQuadFilter[EqPresets.Frequencies.Length][];

        for (var i = 0; i < bands.Length; i++)
        {
            bands[i] = new BiQuadFilter[channels];
            for (var ch = 0; ch < channels; ch++)
                bands[i][ch] = BiQuadFilter.PeakingEQ(rate, EqPresets.Frequencies[i], 1.0f, _gainsDb[i]);
        }

        return bands;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var read = _source.Read(buffer, offset, count);
        var channels = Math.Max(1, WaveFormat.Channels);

        lock (_lock)
        {
            for (var n = 0; n < read; n++)
            {
                var ch = n % channels;
                var sample = buffer[offset + n];
                for (var i = 0; i < _filters.Length; i++)
                    sample = _filters[i][ch].Transform(sample);
                buffer[offset + n] = sample;
            }
        }

        return read;
    }
}
