using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RemoteMouse.PC;

public partial class SetPasswordWindow : Window
{
    public bool Saved { get; private set; }
    public string? NewPassword { get; private set; }

    public SetPasswordWindow()
    {
        InitializeComponent();
        EditPassword.PasswordChanged += (_, _) => HideError();
        EditPasswordConfirm.PasswordChanged += (_, _) => HideError();
    }

    private void ShowError(string message)
    {
        TbError.Text = message;
        TbError.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        TbError.Text = "";
        TbError.Visibility = Visibility.Collapsed;
    }

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        var pwd = EditPassword.Password;
        var confirm = EditPasswordConfirm.Password;
        if (pwd.Length < 6)
        {
            ShowError("Пароль должен содержать не менее 6 символов.");
            return;
        }
        if (pwd != confirm)
        {
            ShowError("Пароли не совпадают.");
            return;
        }
        HideError();
        NewPassword = pwd;
        Saved = true;
        Close();
    }

    private void BtnReset_OnClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("Вы точно хотите сбросить ваш пароль?", "Remote", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        NewPassword = "";
        Saved = true;
        Close();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void BtnCloseChrome_OnClick(object sender, RoutedEventArgs e) => Close();
}
