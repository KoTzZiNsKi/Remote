using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;

namespace RemoteMouse.PC;

public partial class SetupWindow : Window
{
    private int _step;

    private static string GetPhotoPath(int step)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var nextToExe = Path.Combine(baseDir, $"photo{step + 1}.png");
        if (File.Exists(nextToExe)) return nextToExe;
        var pcCsharpDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        return Path.Combine(pcCsharpDir, $"photo{step + 1}.png");
    }

    private static System.Windows.Controls.Image? TryAddStepImage(StackPanel panel, int step)
    {
        var path = GetPhotoPath(step);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try
        {
            var uri = new Uri(path, UriKind.Absolute);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();
            var img = new System.Windows.Controls.Image
            {
                Source = bitmap,
                MaxHeight = 140,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 0, 16)
            };
            panel.Children.Insert(0, img);
            return img;
        }
        catch { return null; }
    }

    public SetupWindow()
    {
        InitializeComponent();
        ShowStep(0);
    }

    private void ShowStep(int step)
    {
        _step = step;
        Dot1.Fill = step >= 0 ? new SolidColorBrush(WpfColor.FromRgb(0x21, 0x96, 0xF3)) : new SolidColorBrush(WpfColor.FromRgb(0xE0, 0xE0, 0xE0));
        Dot2.Fill = step >= 1 ? new SolidColorBrush(WpfColor.FromRgb(0x21, 0x96, 0xF3)) : new SolidColorBrush(WpfColor.FromRgb(0xE0, 0xE0, 0xE0));
        Dot3.Fill = step >= 2 ? new SolidColorBrush(WpfColor.FromRgb(0x21, 0x96, 0xF3)) : new SolidColorBrush(WpfColor.FromRgb(0xE0, 0xE0, 0xE0));
        BtnBack.Visibility = step > 0 ? Visibility.Visible : Visibility.Collapsed;

        var panel = new StackPanel();
        TryAddStepImage(panel, step);
        switch (step)
        {
            case 0:
                panel.Children.Add(new TextBlock
                {
                    Text = "Добро пожаловать в Remote",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
                panel.Children.Add(new TextBlock
                {
                    Text = "Remote позволяет управлять этим компьютером с помощью телефона или планшета.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 0)
                });
                BtnNext.Content = "Далее";
                BtnNext.Visibility = Visibility.Visible;
                break;
            case 1:
                panel.Children.Add(new TextBlock
                {
                    Text = "Подключите ваш телефон",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
                panel.Children.Add(new TextBlock
                {
                    Text = "Убедитесь, что компьютер и телефон находятся в одной сети. Откройте приложение Remote на телефоне для подключения.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
                panel.Children.Add(new TextBlock { Text = "Ещё не установили приложение?", Margin = new Thickness(0, 0, 0, 4) });
                var linkBtn = new WpfButton
                {
                    Content = "Скачать Remote для телефона",
                    Background = WpfBrushes.Transparent,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(0x21, 0x96, 0xF3)),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    Padding = new Thickness(0)
                };
                linkBtn.Click += (_, _) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/KoTzZiNsKi/Remote/releases") { UseShellExecute = true }); } catch { } };
                panel.Children.Add(linkBtn);
                BtnNext.Content = "Далее";
                BtnNext.Visibility = Visibility.Visible;
                break;
            case 2:
                panel.Children.Add(new TextBlock
                {
                    Text = "Remote работает в фоновом режиме",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
                panel.Children.Add(new TextBlock
                {
                    Text = "Держите приложение запущенным для подключения к этому компьютеру. Вы найдёте его в системном трее.",
                    TextWrapping = TextWrapping.Wrap
                });
                BtnNext.Content = "Готово";
                BtnNext.Visibility = Visibility.Visible;
                break;
        }
        StepContent.Content = panel;
    }

    private void BtnNext_OnClick(object sender, RoutedEventArgs e)
    {
        if (_step < 2)
            ShowStep(_step + 1);
        else
            Close();
    }

    private void BtnBack_OnClick(object sender, RoutedEventArgs e)
    {
        if (_step > 0)
            ShowStep(_step - 1);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void BtnCloseChrome_OnClick(object sender, RoutedEventArgs e) => Close();
}
