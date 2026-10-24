using System;
using System.Threading.Tasks;
using System.Windows;
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

    private void BtnQuickAdmin_Click(object sender, MouseButtonEventArgs e)
    {
        TxtUsername.Text = "admin";
        TxtPassword.Password = "admin123";
        TxtStatus.Text = "Đã chọn tài khoản Quản Trị Viên (Admin).";
        TxtStatus.Foreground = System.Windows.Media.Brushes.LightSkyBlue;
    }

    private void BtnQuickStaff_Click(object sender, MouseButtonEventArgs e)
    {
        TxtUsername.Text = "nhanvien";
        TxtPassword.Password = "nhanvien123";
        TxtStatus.Text = "Đã chọn tài khoản Thu Ngân (Staff).";
        TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
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
            "• Tài khoản Quản trị: admin / admin123\n" +
            "• Tài khoản Thu ngân: nhanvien / nhanvien123",
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
            TxtStatus.Text = "Vui lòng nhập đầy đủ Username và Password.";
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

            // Short smooth delay
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