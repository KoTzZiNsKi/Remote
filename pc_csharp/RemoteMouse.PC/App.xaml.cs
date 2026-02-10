using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace RemoteMouse.PC;

public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon? _tray;
    private RemoteMouseServer? _server;
    private AppConfig _config = null!;
    private Window? _currentSecondary;
    private Mutex? _singleInstanceMutex;

    internal static AppConfig Config => (Current as App)!._config;
    internal static RemoteMouseServer? Server => (Current as App)!._server;

    private void CloseSecondaryAndHideMain()
    {
        _currentSecondary?.Close();
        _currentSecondary = null;
    }

    private void RestartServerWithConfig()
    {
        _server?.Stop();
        _server = new RemoteMouseServer(_config.Password, _config.TcpPort, _config.UdpPort, _ => { }, remote =>
        {
            _config.LastConnectedDevice = remote;
            _config.Save();
        }, () => _config.SilentMode, null, null, () => PcInfo.GetModel());
        _server.Start();
    }

    internal void ShowSettings()
    {
        var prev = _currentSecondary;
        var w = new SettingsWindow(_config, () => _server != null, RestartServerWithConfig);
        w.Closed += (_, _) => { if (_currentSecondary == w) _currentSecondary = null; };
        _currentSecondary = w;
        w.Show();
        prev?.Close();
    }

    internal void ShowIpAndQr(int port, string? password)
    {
        var prev = _currentSecondary;
        var w = new IpAndQrWindow(port, password);
        w.Closed += (_, _) => { if (_currentSecondary == w) _currentSecondary = null; };
        _currentSecondary = w;
        w.Show();
        prev?.Close();
    }

    internal void ShowSetup()
    {
        var prev = _currentSecondary;
        var w = new SetupWindow();
        w.Closed += (_, _) => { if (_currentSecondary == w) _currentSecondary = null; };
        _currentSecondary = w;
        w.Show();
        prev?.Close();
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = (Exception)args.ExceptionObject;
            _ = System.Windows.MessageBox.Show(ex.ToString(), "Remote — ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(args.Exception.ToString(), "Remote — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        try
        {
            _singleInstanceMutex = new Mutex(true, "RemoteMouse.PC.SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                System.Windows.MessageBox.Show("Приложение уже запущено.", "Remote", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
            _config = AppConfig.Load();
            if (_config.LaunchAtStartup)
                StartupManager.SetStartup(true);
            else
                StartupManager.SetStartup(false);
            DesktopShortcutHelper.Ensure(_config.DesktopShortcut);

            _server = new RemoteMouseServer(_config.Password, _config.TcpPort, _config.UdpPort, _ => { }, remote =>
            {
                _config.LastConnectedDevice = remote;
                _config.Save();
            }, () => _config.SilentMode, null, null, () => PcInfo.GetModel());
            _server.Start();

            _tray = new System.Windows.Forms.NotifyIcon
            {
                Icon = AppIcon.GetTrayIcon() ?? System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "Remote"
            };
            _tray.DoubleClick += (_, _) => ShowSettings();
            _tray.MouseClick += (_, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    var p = System.Windows.Forms.Cursor.Position;
                    Dispatcher.BeginInvoke(new Action(() => ShowTrayMenu(p.X, p.Y)));
                }
            };
            ShowSettings();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "Remote — ошибка при запуске", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ShowTrayMenu(int x, int y)
    {
        var menu = new TrayMenuWindow
        {
            OnShowIp = () =>
            {
                ShowIpAndQr(_config.TcpPort, _config.Password);
            },
            OnSettings = () => ShowSettings(),
            OnUpdates = () =>
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0";
                    var (hasNew, version, url) = await UpdateChecker.CheckAsync(current);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (version == null)
                        {
                            System.Windows.MessageBox.Show("Не удалось проверить обновления.", "Remote", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (!hasNew)
                        {
                            System.Windows.MessageBox.Show("У вас уже установлена последняя версия программы", "Remote", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        if (string.IsNullOrEmpty(url))
                        {
                            System.Windows.MessageBox.Show("Ссылка на загрузку не найдена.", "Remote", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        var currentExe = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RemoteMouse.PC.exe");
                                var tempNew = Path.Combine(Path.GetTempPath(), "RemoteMouse.PC.new.exe");
                                _ = System.Threading.Tasks.Task.Run(async () =>
                                {
                                    var ok = await UpdateChecker.DownloadToFileAsync(url, tempNew);
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        try
                                        {
                                            if (!ok)
                                            {
                                                System.Windows.MessageBox.Show("Не удалось скачать обновление.", "Remote", MessageBoxButton.OK, MessageBoxImage.Warning);
                                                return;
                                            }
                                            var pid = Environment.ProcessId;
                                            var psPath = Path.Combine(Path.GetTempPath(), "RemoteMouse.PC.update.ps1");
                                            var ps = $@"$p = Get-Process -Id {pid} -ErrorAction SilentlyContinue; if ($p) {{ $p.WaitForExit() }}; Start-Sleep -Seconds 1; Move-Item -LiteralPath '{tempNew.Replace("'", "''")}' -Destination '{currentExe.Replace("'", "''")}' -Force; Start-Process -FilePath '{currentExe.Replace("'", "''")}'";
                                            File.WriteAllText(psPath, ps);
                                            var si = new ProcessStartInfo
                                            {
                                                FileName = "powershell.exe",
                                                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{psPath}\"",
                                                UseShellExecute = false,
                                                CreateNoWindow = true
                                            };
                                            Process.Start(si);
                                            _server?.Stop();
                                            _tray!.Visible = false;
                                            _tray.Dispose();
                                            Shutdown();
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Windows.MessageBox.Show($"Ошибка обновления: {ex.Message}", "Remote", MessageBoxButton.OK, MessageBoxImage.Error);
                                        }
                                    });
                                });
                    });
                });
            },
            OnHelpStart = () => ShowSetup(),
            OnFaq = () =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/KoTzZiNsKi/Remote") { UseShellExecute = true }); } catch { }
            },
            OnExit = () =>
            {
                _server?.Stop();
                _tray!.Visible = false;
                _tray.Dispose();
                Shutdown();
            }
        };
        menu.ShowNearTray(x, y);
    }

    private void App_OnExit(object sender, ExitEventArgs e)
    {
        _server?.Stop();
        _tray?.Dispose();
    }
}
