using System.IO;
using System.Text.Json;

namespace DSRemote.Models;

public class AppConfig
{
    public List<string> GameFolders { get; set; } = new();
    public string AccentColor { get; set; } = "#32CD32";
    public bool SetupComplete { get; set; }
    public bool UsePcAsTopScreen { get; set; }

    public string EmulatorPath
    {
        get
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DSRemote", "emulators");

            // Check directories in priority order
            string[] searchDirs =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmulatorFiles", "azahar"),
                Path.Combine(baseDir, "azahar"),
                Path.Combine(baseDir, "lime3ds"), // fallback to old Lime3DS directory
            };

            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;

                // Copy bundled files to AppData on first run
                if (dir == searchDirs[0] && !Directory.Exists(searchDirs[1]))
                    CopyFiles(dir, searchDirs[1]);

                var exe = FindAzaharExe(dir);
                if (exe != null) return exe;
            }

            return "";
        }
    }

    private static string? FindAzaharExe(string dir)
    {
        // Prefer azahar.exe in any subdirectory
        var allExes = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories);

        var preferred = allExes.FirstOrDefault(f =>
            Path.GetFileName(f).Equals("azahar.exe", StringComparison.OrdinalIgnoreCase));
        if (preferred != null) return preferred;

        // Then try any non-CLI exe (prefer GUI names)
        foreach (var name in new[] { "azahar-qt.exe", "lime3ds-qt.exe", "lime3ds.exe", "citra-qt.exe" })
        {
            var match = allExes.FirstOrDefault(f =>
                Path.GetFileName(f).Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        // Last resort: any exe without "cli" in the name
        return allExes.FirstOrDefault(f => !Path.GetFileNameWithoutExtension(f)
            .Contains("cli", StringComparison.OrdinalIgnoreCase));
    }

    private static void CopyFiles(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
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
