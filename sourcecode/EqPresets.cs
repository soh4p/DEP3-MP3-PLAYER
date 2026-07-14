namespace DEP3;

static class EqPresets
{
    // 10-band center frequencies (Hz)
    public static readonly float[] Frequencies =
        [32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000];

    public static readonly string[] Names =
    [
        "Flat", "Rock", "Pop", "Jazz", "Classical",
        "Electronic", "Hip-Hop", "Bass Boost", "Vocal", "Metal"
    ];

    public static float[] GetGains(string preset) => preset switch
    {
        "Rock" => [4, 3, 1, 0, -1, 1, 3, 4, 4, 3],
        "Pop" => [-1, 0, 2, 3, 2, 0, -1, -1, 1, 2],
        "Jazz" => [2, 1, 0, 1, -1, -1, 0, 1, 2, 3],
        "Classical" => [3, 2, 0, 0, 0, 0, -1, -1, 2, 3],
        "Electronic" => [5, 4, 1, 0, -2, 1, 2, 3, 4, 4],
        "Hip-Hop" => [6, 5, 2, 1, -1, -1, 1, 2, 2, 3],
        "Bass Boost" => [8, 6, 4, 2, 0, 0, 0, 0, 0, 0],
        "Vocal" => [-2, -1, 0, 2, 4, 4, 3, 1, 0, -1],
        "Metal" => [4, 3, 0, -1, 0, 2, 4, 5, 4, 3],
        _ => [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
    };
}
