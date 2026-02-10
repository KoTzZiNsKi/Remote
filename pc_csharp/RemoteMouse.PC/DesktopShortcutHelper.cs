using System.Diagnostics;
using System.IO;

namespace RemoteMouse.PC;

public static class DesktopShortcutHelper
{
    private const string ShortcutName = "Remote.lnk";

    public static string GetShortcutPath()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        return Path.Combine(desktop, ShortcutName);
    }

    public static bool Exists()
    {
        return File.Exists(GetShortcutPath());
    }

    public static void Ensure(bool add)
    {
        if (add)
            Create();
        else
            Remove();
    }

    public static void Create()
    {
        var exePath = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RemoteMouse.PC.exe");
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return;
        var lnkPath = GetShortcutPath();
        if (CreateViaCom(exePath, lnkPath)) return;
        CreateViaPowerShell(exePath, lnkPath);
    }

    private static bool CreateViaCom(string exePath, string lnkPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return false;
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return false;
            dynamic shortcut = shell.CreateShortcut(lnkPath);
            shortcut.TargetPath = exePath;
            shortcut.Description = "Remote";
            shortcut.Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void CreateViaPowerShell(string exePath, string lnkPath)
    {
        try
        {
            var script = @"
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut($env:REMOTE_LNK)
$sc.TargetPath = $env:REMOTE_EXE
$sc.Description = 'Remote'
$sc.Save()
";
            var tmp = Path.Combine(Path.GetTempPath(), "Remote_create_shortcut.ps1");
            File.WriteAllText(tmp, script);
            var si = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{tmp}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            si.Environment["REMOTE_EXE"] = exePath;
            si.Environment["REMOTE_LNK"] = lnkPath;
            using (var p = Process.Start(si))
                p?.WaitForExit(5000);
            try { File.Delete(tmp); } catch { }
        }
        catch { }
    }

    public static void Remove()
    {
        try
        {
            var path = GetShortcutPath();
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }
}
