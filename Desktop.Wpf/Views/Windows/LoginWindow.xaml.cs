using System;
using System.Windows;
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

    private void BtnQuickAdmin_Click(object sender, RoutedEventArgs e)
    {
        TxtUsername.Text = "admin";
        TxtPassword.Password = "admin123";
        TxtStatus.Text = "Đã chọn tài khoản Quản Trị Viên.";
        TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
    }

    private void BtnQuickStaff_Click(object sender, RoutedEventArgs e)
    {
        TxtUsername.Text = "nhanvien";
        TxtPassword.Password = "nhanvien123";
        TxtStatus.Text = "Đã chọn tài khoản Nhân Viên Thu Ngân.";
        TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string username = TxtUsername.Text ?? "";
        string password = TxtPassword.Password ?? "";

        BtnLogin.IsEnabled = false;
        TxtStatus.Text = "Đang xác thực thông tin tài khoản...";
        TxtStatus.Foreground = System.Windows.Media.Brushes.Blue;

        var result = await _authService.LoginAsync(username, password);

        if (result.Success)
        {
            TxtStatus.Text = "✓ " + result.Message;
            TxtStatus.Foreground = System.Windows.Media.Brushes.Green;

            // Short delay to show success
            await Task.Delay(300);

            if (result.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                result.Role.Equals("Cashier", StringComparison.OrdinalIgnoreCase) ||
                result.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
            {
                // Open Employee Workstation
                var empWin = new EmployeeMainWindow();
                empWin.Show();
                this.Close();
            }
            else
            {
                // Open Admin Workstation
                var adminWin = new AdminMainWindow();
                adminWin.Show();
                this.Close();
            }
        }
        else
        {
            TxtStatus.Text = "✕ " + result.Message;
            TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
            BtnLogin.IsEnabled = true;
        }
    }
}
