using System.IO;

namespace DSRemote.Models;

public class KidsModeManager
{
    private static readonly string PasswordFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DSRemote", "kidsmode.dat");

    private static readonly string LocalBgImage = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Resources", "kids_bg.png");

    private static readonly string AppDataBgImage = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DSRemote", "kids_bg.png");

    private static readonly Dictionary<char, char> SymbolMap = new()
    {
        ['1'] = '!', ['2'] = '@', ['3'] = '#', ['4'] = '$',
        ['5'] = '%', ['6'] = '^', ['7'] = '&', ['8'] = '*',
        ['9'] = '(', ['0'] = ')'
    };

    private static readonly Dictionary<char, char> ReverseSymbolMap = SymbolMap
        .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public bool HasPassword => File.Exists(PasswordFile);
    public int SelectedAge { get; set; } = 6;

    public static string EncodePassword(string digits)
    {
        return new string(digits.Select(c => SymbolMap.TryGetValue(c, out var s) ? s : c).ToArray());
    }

    public string? LoadPassword()
    {
        try
        {
            if (!HasPassword) return null;
            return File.ReadAllText(PasswordFile).Trim();
        }
        catch { return null; }
    }

    public bool VerifyPassword(string input)
    {
        var encoded = EncodePassword(input);
        var stored = LoadPassword();
        return stored != null && stored == encoded;
    }

    public void SavePassword(string digits)
    {
        var dir = Path.GetDirectoryName(PasswordFile);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(PasswordFile, EncodePassword(digits));
    }

    public void ClearPassword()
    {
        try { if (File.Exists(PasswordFile)) File.Delete(PasswordFile); }
        catch { }
    }

    public string? BackgroundImagePath =>
        File.Exists(LocalBgImage) ? LocalBgImage :
        File.Exists(AppDataBgImage) ? AppDataBgImage : null;
}
