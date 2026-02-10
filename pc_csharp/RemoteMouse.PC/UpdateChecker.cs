using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RemoteMouse.PC;

public static class UpdateChecker
{
    public const string GitHubReleasesUrl = "https://api.github.com/repos/KoTzZiNsKi/Remote/releases/latest";

    public static async Task<(bool hasNew, string? latestVersion, string? downloadUrl)> CheckAsync(string currentVersion)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Remote/1.0");
            var json = await client.GetStringAsync(GitHubReleasesUrl);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var version = Regex.Replace(tagName, @"^v", "", RegexOptions.IgnoreCase);
            var assets = root.GetProperty("assets");
            string? url = null;
            foreach (var a in assets.EnumerateArray())
            {
                var name = a.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    url = a.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
            var hasNew = string.Compare(version, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
            return (hasNew, version, url);
        }
        catch
        {
            return (false, null, null);
        }
    }

    public static async Task<bool> DownloadToFileAsync(string url, string destinationPath)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Remote/1.0");
            var bytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(destinationPath, bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
