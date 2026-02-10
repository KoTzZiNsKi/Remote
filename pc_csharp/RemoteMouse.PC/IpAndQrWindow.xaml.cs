using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace RemoteMouse.PC;

public partial class IpAndQrWindow : Window
{
    private readonly List<string> _ips;
    private readonly int _port;
    private readonly string? _password;
    private int _index;

    public IpAndQrWindow(int port, string? password)
    {
        InitializeComponent();
        _port = port;
        _password = password;
        _ips = RemoteMouseServer.GetLocalIps();
        _index = 0;
        var showArrows = _ips.Count > 1;
        BtnPrevIp.Visibility = BtnNextIp.Visibility = showArrows ? Visibility.Visible : Visibility.Collapsed;
        RefreshCurrent();
    }

    private void RefreshCurrent()
    {
        var ip = _ips[_index];
        TbIp.Text = ip;
        if (_ips.Count > 1)
        {
            BtnPrevIp.IsEnabled = _index > 0;
            BtnNextIp.IsEnabled = _index < _ips.Count - 1;
        }
        try
        {
            var url = $"remotemouse://{ip}:{_port}";
            if (!string.IsNullOrEmpty(_password)) url += $"?pwd={Uri.EscapeDataString(_password)}";
            using var gen = new QRCoder.QRCodeGenerator();
            using var qrData = gen.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.M);
            using var qrCode = new QRCoder.QRCode(qrData);
            using var bmp = qrCode.GetGraphic(6);
            using var ms = new System.IO.MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            QrImage.Source = bi;
        }
        catch { }
    }

    private void BtnPrevIp_OnClick(object sender, RoutedEventArgs e)
    {
        if (_index <= 0) return;
        _index--;
        RefreshCurrent();
    }

    private void BtnNextIp_OnClick(object sender, RoutedEventArgs e)
    {
        if (_index >= _ips.Count - 1) return;
        _index++;
        RefreshCurrent();
    }

    private void BtnCopy_OnClick(object sender, RoutedEventArgs e)
    {
        try { System.Windows.Clipboard.SetText($"{_ips[_index]}:{_port}"); } catch { }
    }

    private void BtnDone_OnClick(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void BtnCloseChrome_OnClick(object sender, RoutedEventArgs e) => Close();
}
