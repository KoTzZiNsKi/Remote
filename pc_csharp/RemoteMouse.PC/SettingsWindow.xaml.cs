using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RemoteMouse.PC;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly Func<bool> _isServerRunning;
    private readonly Action _refreshPasswordHint;

    public SettingsWindow(AppConfig config, Func<bool> isServerRunning, Action refreshPasswordHint)
    {
        InitializeComponent();
        _config = config;
        _isServerRunning = isServerRunning;
        _refreshPasswordHint = refreshPasswordHint;
        var name = GetSystemLikeComputerName();
        var ip = RemoteMouseServer.GetLocalIp();
        TbPcName.Text = $"{name} ({ip})";
        var model = PcInfo.GetModel();
        if (!string.IsNullOrEmpty(model))
        {
            TbPcModel.Text = "Модель: " + model;
            TbPcModel.Visibility = Visibility.Visible;
        }
        TbLastDevice.Text = string.IsNullOrEmpty(_config.LastConnectedDevice) ? "Нет" : _config.LastConnectedDevice;
        CbLaunchAtStartup.IsChecked = _config.LaunchAtStartup;
        CbDesktopShortcut.IsChecked = _config.DesktopShortcut;
        CbSilentMode.IsChecked = _config.SilentMode;
        TbVersion.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0";
        UpdatePasswordHint();
        if (AppIcon.GetWindowIcon() is { } icon)
        {
            TitleIcon.Source = icon;
            TitleIconBorder.Visibility = Visibility.Visible;
        }
        else
            TitleIconBorder.Visibility = Visibility.Collapsed;
    }

    private static string GetSystemLikeComputerName()
    {
        var raw = Environment.MachineName;
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(raw.ToLowerInvariant());
    }

    private void UpdatePasswordHint()
    {
        bool hasPassword = !string.IsNullOrEmpty(_config.Password);
        TbPasswordHint.Text = hasPassword
            ? "Пароль установлен."
            : "Пароль не установлен. В публичных сетях рекомендуется установить пароль.";
        BtnSetPassword.Content = hasPassword ? "Изменить пароль" : "Установить пароль";
    }

    private void BtnShowQr_OnClick(object sender, RoutedEventArgs e)
    {
        (System.Windows.Application.Current as App)!.ShowIpAndQr(_config.TcpPort, _config.Password);
        Close();
    }

    private void CbLaunch_Changed(object sender, RoutedEventArgs e)
    {
        var on = CbLaunchAtStartup.IsChecked == true;
        _config.LaunchAtStartup = on;
        _config.Save();
        if (on)
            StartupManager.SetStartup(true);
        else
            StartupManager.SetStartup(false);
    }

    private void CbDesktopShortcut_Changed(object sender, RoutedEventArgs e)
    {
        var on = CbDesktopShortcut.IsChecked == true;
        _config.DesktopShortcut = on;
        _config.Save();
        DesktopShortcutHelper.Ensure(on);
    }

    private void BtnSetPassword_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new SetPasswordWindow { Owner = this };
        dlg.ShowDialog();
        if (dlg.Saved)
        {
            _config.Password = dlg.NewPassword ?? "";
            _config.Save();
            UpdatePasswordHint();
            _refreshPasswordHint();
        }
    }

    private void CbSilent_Changed(object sender, RoutedEventArgs e)
    {
        _config.SilentMode = CbSilentMode.IsChecked == true;
        _config.Save();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void BtnCloseChrome_OnClick(object sender, RoutedEventArgs e) => Close();

    }
