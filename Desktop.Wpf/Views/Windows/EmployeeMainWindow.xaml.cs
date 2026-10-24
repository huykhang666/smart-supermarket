using System.Windows;
using SmartSupermarket.POS.Services;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS.Views.Windows;

public partial class EmployeeMainWindow : FluentWindow
{
    public EmployeeMainWindow()
    {
        InitializeComponent();
        TxtStaffProfile.Text = $"👤 {AuthService.CurrentUser.FullName} ({AuthService.CurrentUser.Username})";
        Loaded += (s, e) =>
        {
            RootNavigation.Navigate(typeof(Pages.PosPage));
        };
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var confirm = System.Windows.MessageBox.Show("Xác nhận kết ca và đăng xuất khỏi trạm POS thu ngân?", "Kết ca làm việc", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm == System.Windows.MessageBoxResult.Yes)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}
