using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RemoteMouse.PC;

public partial class TrayMenuWindow : Window
{
    private bool _isClosing;

    public Action? OnShowIp { get; set; }
    public Action? OnSettings { get; set; }
    public Action? OnUpdates { get; set; }
    public Action? OnHelpStart { get; set; }
    public Action? OnFaq { get; set; }
    public Action? OnExit { get; set; }

    private int _trayClickX, _trayClickY;

    public TrayMenuWindow()
    {
        InitializeComponent();
    }

    public void ShowNearTray(int clickScreenX, int clickScreenY)
    {
        _trayClickX = clickScreenX;
        _trayClickY = clickScreenY;
        Opacity = 0;
        Show();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        var dpi = VisualTreeHelper.GetDpi(this);
        double scaleX = dpi.DpiScaleX;
        double scaleY = dpi.DpiScaleY;

        double clickX = _trayClickX / scaleX;
        double clickY = _trayClickY / scaleY;

        double w = ActualWidth;
        double h = ActualHeight;

        var screen = System.Windows.Forms.Screen.FromPoint(
            new System.Drawing.Point(_trayClickX, _trayClickY));

        var workPx = screen.WorkingArea;
        var work = new Rect(
            workPx.Left / scaleX,
            workPx.Top / scaleY,
            workPx.Width / scaleX,
            workPx.Height / scaleY);

        double x = clickX;
        double y = clickY - h;

        if (y < work.Top) y = clickY;
        if (x + w > work.Right) x = clickX - w;

        x = Math.Max(work.Left, Math.Min(x, work.Right - w));
        y = Math.Max(work.Top, Math.Min(y, work.Bottom - h));

        Left = x;
        Top = y;

        Opacity = 1;
        Activate();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        Close();
    }

    private void CloseMenu()
    {
        if (_isClosing) return;
        _isClosing = true;
        Close();
    }

    private void BtnShowIp_Click(object sender, RoutedEventArgs e) { CloseMenu(); OnShowIp?.Invoke(); }
    private void BtnSettings_Click(object sender, RoutedEventArgs e) { CloseMenu(); OnSettings?.Invoke(); }
    private void BtnUpdates_Click(object sender, RoutedEventArgs e) { CloseMenu(); OnUpdates?.Invoke(); }
    private void BtnHelp_Click(object sender, RoutedEventArgs e)
    {
        PanelHelpSub.Visibility = PanelHelpSub.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BtnHelpStart_Click(object sender, RoutedEventArgs e) { CloseMenu(); OnHelpStart?.Invoke(); }
    private void BtnFaq_Click(object sender, RoutedEventArgs e) { CloseMenu(); OnFaq?.Invoke(); }
    private void BtnExit_Click(object sender, RoutedEventArgs e) { CloseMenu(); OnExit?.Invoke(); }
}
