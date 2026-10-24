using System.Windows;
using SmartSupermarket.POS.Services;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS.Views.Windows;

public partial class AdminMainWindow : FluentWindow
{
    public AdminMainWindow()
    {
        InitializeComponent();
        TxtUserProfile.Text = $"👑 {AuthService.CurrentUser.FullName} ({AuthService.CurrentUser.Role})";
        Loaded += (s, e) =>
        {
            RootNavigation.Navigate(typeof(Pages.DashboardPage));
        };
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var confirm = System.Windows.MessageBox.Show("Xác nhận đăng xuất khỏi tài khoản Quản trị?", "Đăng xuất", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm == System.Windows.MessageBoxResult.Yes)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}
