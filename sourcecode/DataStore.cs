using System.Text.Json;

namespace DEP3;

static class DataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string RootDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DEP3");

    private static string DataPath => Path.Combine(RootDir, "data.json");

    public static AppData Load()
    {
        try
        {
            if (!File.Exists(DataPath))
                return new AppData();

            var json = File.ReadAllText(DataPath);
            return JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
        }
        catch
        {
            return new AppData();
        }
    }

    public static void Save(AppData data)
    {
        Directory.CreateDirectory(RootDir);
        File.WriteAllText(DataPath, JsonSerializer.Serialize(data, JsonOptions));
    }
}
