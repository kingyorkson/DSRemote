using System.IO;
using DSRemote.Models;

namespace DSRemote.Services;

public class GameLibraryService
{
    private static readonly string[] Extensions = { ".3ds", ".cci", ".cxi", ".cia", ".3dsx" };

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
            foreach (var ext in Extensions)
            {
                foreach (var file in Directory.EnumerateFiles(folder, $"*{ext}", SearchOption.AllDirectories))
                {
                    games.Add(new GameRom
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FullPath = file,
                        SizeBytes = new FileInfo(file).Length
                    });
                }
            }
        }
        catch { }
        return games;
    }
}
