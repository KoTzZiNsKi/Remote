using Microsoft.Win32;

namespace RemoteMouse.PC;

public static class StartupManager
{
    private const string RunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string ValueName = "Remote";

    public static bool IsInStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            var path = key?.GetValue(ValueName) as string;
            var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            return !string.IsNullOrEmpty(path) && path.Equals(exePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartup(bool enable)
    {
        try
        {
            var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(exePath)) return;
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;
            if (enable)
                key.SetValue(ValueName, exePath);
            else
                key.DeleteValue(ValueName, false);
        }
        catch { }
    }
}
