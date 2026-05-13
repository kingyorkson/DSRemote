using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DSRemote.Models;

namespace DSRemote.Services;

public class EmulatorDownloader
{
    private readonly string _emulatorsDir;

    public EmulatorDownloader()
    {
        _emulatorsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DSRemote", "emulators");
    }

    public string GetEmulatorDir(EmulatorType type) => type switch
    {
        EmulatorType.DS => Path.Combine(_emulatorsDir, "melonDS"),
        EmulatorType.ThreeDS => Path.Combine(_emulatorsDir, "lime3ds"),
        _ => Path.Combine(_emulatorsDir, "melonDS")
    };

    public bool IsDownloaded(EmulatorType type) =>
        Directory.Exists(GetEmulatorDir(type)) &&
        Directory.GetFiles(GetEmulatorDir(type), "*.exe", SearchOption.AllDirectories).Length > 0;

    public async Task<string> DownloadEmulator(EmulatorType type, IProgress<int>? progress = null)
    {
        var dir = GetEmulatorDir(type);
        if (IsDownloaded(type)) return dir;

        Directory.CreateDirectory(dir);
        var zipPath = Path.Combine(_emulatorsDir, "download.zip");

        try
        {
            var (repo, assetPattern) = type switch
            {
                EmulatorType.DS => ("melonDS-emu/melonDS", "win64"),
                EmulatorType.ThreeDS => ("Lime3DS/Lime3DS", "windows-msvc"),
                _ => ("melonDS-emu/melonDS", "win64")
            };

            var downloadUrl = await GetLatestAssetUrl(repo, assetPattern);
            if (string.IsNullOrEmpty(downloadUrl))
                throw new Exception($"Could not find download for {type}");

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DSRemote");

            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1;
            using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var stream = await response.Content.ReadAsStreamAsync();

            var buffer = new byte[81920];
            long read = 0;
            int bytes;
            while ((bytes = await stream.ReadAsync(buffer)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, bytes));
                read += bytes;
                if (total > 0 && progress != null)
                    progress.Report((int)(read * 100 / total));
            }

            await fs.FlushAsync();
            fs.Close();

            ZipFile.ExtractToDirectory(zipPath, dir, overwriteFiles: true);
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }

        return dir;
    }

    private static async Task<string?> GetLatestAssetUrl(string repo, string pattern)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DSRemote");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        try
        {
            var json = await client.GetFromJsonAsync<JsonElement>(
                $"https://api.github.com/repos/{repo}/releases/latest");

            if (json.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".zip"))
                    {
                        return asset.GetProperty("browser_download_url").GetString();
                    }
                }
            }
        }
        catch { }

        return null;
    }
}
