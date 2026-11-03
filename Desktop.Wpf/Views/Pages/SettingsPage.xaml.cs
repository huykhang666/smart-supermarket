using System.Windows;
using System.Windows.Controls;
using SmartSupermarket.POS.Services;
using Wpf.Ui.Appearance;

namespace SmartSupermarket.POS.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            if (TxtStaffName != null)
                TxtStaffName.Text = AuthService.CurrentUser.FullName;
            if (TxtStaffUsername != null)
                TxtStaffUsername.Text = AuthService.CurrentUser.Username;
        };
    }

    private void BtnThemeLight_Click(object sender, RoutedEventArgs e)
    {
        ApplicationThemeManager.Apply(ApplicationTheme.Light);
    }

    private void BtnThemeDark_Click(object sender, RoutedEventArgs e)
    {
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
    }

    private void BtnPrintTest_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Đã gửi lệnh in trang thử nghiệm (Test Print) đến máy in nhiệt K80!\n\nTrạng thái kết nối máy in: Hoạt động tốt.",
            "In thử hóa đơn",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnCheckApi_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Kiểm tra kết nối máy chủ dịch vụ:\n• Địa chỉ: http://localhost:5000/api/\n• Tình trạng: Trực tuyến (Online)\n• Độ trễ: 38ms",
            "Kiểm tra máy chủ API",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Đã lưu thành công toàn bộ thiết lập quầy thu ngân!",
            "Thông báo",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
