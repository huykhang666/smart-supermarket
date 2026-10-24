using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;

namespace SmartSupermarket.POS.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void BtnThemeLight_Click(object sender, RoutedEventArgs e)
    {
        ApplicationThemeManager.Apply(ApplicationTheme.Light);
    }

    private void BtnThemeDark_Click(object sender, RoutedEventArgs e)
    {
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
    }

    private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Đã lưu thiết lập địa chỉ API Backend!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
