using System.IO;
using System.Text.Json;

namespace DSRemote.Models;

public class AppConfig
{
    public EmulatorType EmulatorType { get; set; } = EmulatorType.DS;
    public List<string> GameFolders { get; set; } = new();
    public string AccentColor { get; set; } = "#32CD32";
    public string LastConnectedDevice { get; set; } = string.Empty;
    public bool SetupComplete { get; set; }

    public string EmulatorPath
    {
        get
        {
            var dir = EmulatorType switch
            {
                EmulatorType.DS => Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DSRemote", "emulators", "melonDS"),
                EmulatorType.ThreeDS => Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DSRemote", "emulators", "lime3ds"),
                _ => ""
            };
            if (!Directory.Exists(dir)) return "";
            var exe = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
            return exe ?? "";
        }
    }

    public Dictionary<int, int> ButtonMappings { get; set; } = new()
    {
        [0] = (int)'Z',      // A
        [1] = (int)'X',      // B
        [2] = (int)'S',      // X
        [3] = (int)'A',      // Y
        [4] = (int)'Q',      // L
        [5] = (int)'W',      // R
        [6] = 0x0D,          // Start = Enter
        [7] = 0x08,          // Select = Backspace
    };
    public Dictionary<int, int> DPadMappings { get; set; } = new()
    {
        [0] = 0x26,          // Up
        [1] = 0x28,          // Down
        [2] = 0x25,          // Left
        [3] = 0x27,          // Right
    };

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
