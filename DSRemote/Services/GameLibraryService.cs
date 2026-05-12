using System.IO;
using DSRemote.Models;

namespace DSRemote.Services;

public class GameLibraryService
{
    private static readonly string[] DSExtensions = { ".nds", ".dsi", ".gba" };
    private static readonly string[] ThreeDSExtensions = { ".3ds", ".cci", ".cxi", ".cia", ".3dsx" };

    public List<GameRom> ScanFolders(IEnumerable<string> folders)
    {
        var games = new List<GameRom>();
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder)) continue;
            games.AddRange(ScanFolder(folder));
        }
        return games;
    }

    private static List<GameRom> ScanFolder(string folder)
    {
        var games = new List<GameRom>();
        try
        {
            var allExts = DSExtensions.Concat(ThreeDSExtensions).ToArray();
            foreach (var ext in allExts)
            {
                foreach (var file in Directory.EnumerateFiles(folder, $"*{ext}", SearchOption.AllDirectories))
                {
                    var is3DS = ThreeDSExtensions.Contains(Path.GetExtension(file).ToLower());
                    games.Add(new GameRom
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FullPath = file,
                        Platform = is3DS ? EmulatorType.ThreeDS : EmulatorType.DS,
                        SizeBytes = new FileInfo(file).Length
                    });
                }
            }
        }
        catch { }
        return games;
    }
}
