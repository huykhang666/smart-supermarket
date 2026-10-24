using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartSupermarket.POS.Services;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS.Views.Windows;

public partial class LoginWindow : FluentWindow
{
    private readonly AuthService _authService = new();

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PlaceholderUsername != null)
        {
            PlaceholderUsername.Visibility = string.IsNullOrEmpty(TxtUsername.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (PlaceholderPassword != null)
        {
            PlaceholderPassword.Visibility = string.IsNullOrEmpty(TxtPassword.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void LinkForgotPassword_Click(object sender, MouseButtonEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Vui lòng liên hệ Quản trị viên hệ thống (IT Admin) qua nội bộ để đặt lại mật khẩu trạm POS.",
            "Quên mật khẩu",
            System.Windows.MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void LinkNeedHelp_Click(object sender, MouseButtonEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Smart Supermarket POS Workstation v2.0\n" +
            "Hỗ trợ kỹ thuật: support@smartsupermarket.local\n" +
            "Hotline hỗ trợ ca: 1900-8888\n\n" +
            "Hệ thống tự động điều hướng theo phân quyền tài khoản:\n" +
            "• Tài khoản Quản trị viên (Admin) -> Trạm Quản Trị ERP\n" +
            "• Tài khoản Thu ngân (Staff/Cashier) -> Trạm Bán Hàng POS",
            "Trợ giúp hệ thống",
            System.Windows.MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string username = TxtUsername.Text?.Trim() ?? "";
        string password = TxtPassword.Password ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TxtStatus.Text = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu.";
            TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            return;
        }

        BtnLogin.IsEnabled = false;
        TxtStatus.Text = "Đang xác thực thông tin...";
        TxtStatus.Foreground = System.Windows.Media.Brushes.LightSkyBlue;

        var result = await _authService.LoginAsync(username, password);

        if (result.Success)
        {
            TxtStatus.Text = "✓ " + result.Message;
            TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;

            // Short smooth delay to show success
            await Task.Delay(350);

            if (result.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                result.Role.Equals("Cashier", StringComparison.OrdinalIgnoreCase) ||
                result.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
            {
                var empWin = new EmployeeMainWindow();
                empWin.Show();
                this.Close();
            }
            else
            {
                var adminWin = new AdminMainWindow();
                adminWin.Show();
                this.Close();
            }
        }
        else
        {
            TxtStatus.Text = "✕ " + result.Message;
            TxtStatus.Foreground = System.Windows.Media.Brushes.Salmon;
            BtnLogin.IsEnabled = true;
        }
    }
}