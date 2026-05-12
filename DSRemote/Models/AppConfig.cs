using System.IO;
using System.Text.Json;

namespace DSRemote.Models;

public class AppConfig
{
    public string EmulatorPath { get; set; } = string.Empty;
    public EmulatorType EmulatorType { get; set; } = EmulatorType.DS;
    public List<string> GameFolders { get; set; } = new();
    public string AccentColor { get; set; } = "#32CD32";
    public string LastConnectedDevice { get; set; } = string.Empty;
    public bool SetupComplete { get; set; }

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DSRemote", "config.json");

    public static AppConfig Load()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}

public enum EmulatorType
{
    DS,
    ThreeDS
}
