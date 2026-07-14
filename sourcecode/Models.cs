namespace DEP3;

public sealed class TrackInfo
{
    public string Path { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public int PlayCount { get; set; }

    public override string ToString()
    {
        var dur = FormatDuration(Duration);
        var main = string.IsNullOrWhiteSpace(Artist) ? Title : $"{Artist} — {Title}";
        return dur.Length > 0 ? $"{main}  ({dur})" : main;
    }

    private static string FormatDuration(TimeSpan t)
    {
        if (t <= TimeSpan.Zero)
            return "";
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{(int)t.TotalMinutes}:{t.Seconds:D2}";
    }
}

public sealed class Playlist
{
    public string Name { get; set; } = "";
    public List<string> Paths { get; set; } = [];
}

public sealed class AppData
{
    public string? MusicFolder { get; set; }
    public float Volume { get; set; } = 0.7f;
    public string ShuffleMode { get; set; } = "Off";
    public bool AutoPlay { get; set; } = true;
    public Dictionary<string, int> PlayCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Playlist> Playlists { get; set; } = [];

    public int PlaybackDeviceNumber { get; set; } = -1;
    public string? PlaybackDeviceName { get; set; }
    public bool LaunchOnStartup { get; set; }
    public bool MinimizeOnClose { get; set; }
    public string EqualizerPreset { get; set; } = "Flat";
    public int WindowWidth { get; set; } = 900;
    public int WindowHeight { get; set; } = 620;
    public double TotalListenSeconds { get; set; }
}
