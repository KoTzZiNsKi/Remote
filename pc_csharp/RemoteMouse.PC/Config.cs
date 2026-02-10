using System.IO;
using System.Text.Json;

namespace RemoteMouse.PC;

public class AppConfig
{
    public string Password { get; set; } = "";
    public bool RememberClientPassword { get; set; }
    public int TcpPort { get; set; } = 1978;
    public int UdpPort { get; set; } = 1978;
    public string LastConnectedDevice { get; set; } = "";
    public bool LaunchAtStartup { get; set; }
    public bool SilentMode { get; set; }
    public bool DesktopShortcut { get; set; }

    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".remote_mouse");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            if (!File.Exists(ConfigPath)) return new AppConfig();
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
