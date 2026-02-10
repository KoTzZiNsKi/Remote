using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace RemoteMouse.PC;

public static class AppIcon
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static string GetIconPath()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        if (File.Exists(Path.Combine(dir, "icon.png"))) return Path.Combine(dir, "icon.png");
        if (File.Exists(Path.Combine(dir, "icon.jpg"))) return Path.Combine(dir, "icon.jpg");
        return "";
    }

    public static Icon? GetTrayIcon()
    {
        var path = GetIconPath();
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            using var src = new Bitmap(path);
            const int size = 32;
            using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                using (var clipPath = new GraphicsPath())
                {
                    clipPath.AddEllipse(0, 0, size, size);
                    g.SetClip(clipPath);
                    g.DrawImage(src, 0, 0, size, size);
                }
            }
            var hicon = bmp.GetHicon();
            var icon = (Icon)Icon.FromHandle(hicon).Clone();
            DestroyIcon(hicon);
            return icon;
        }
        catch
        {
            return null;
        }
    }

    public static System.Windows.Media.ImageSource? GetWindowIcon()
    {
        var path = GetIconPath();
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            var uri = new Uri(path, UriKind.Absolute);
            return BitmapFrame.Create(uri);
        }
        catch
        {
            return null;
        }
    }
}
