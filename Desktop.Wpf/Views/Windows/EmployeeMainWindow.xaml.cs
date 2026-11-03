using System.Windows;
using System.Windows.Controls;
using SmartSupermarket.POS.Services;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS.Views.Windows;

public partial class EmployeeMainWindow : FluentWindow
{
    public EmployeeMainWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            NavigateTo(new Pages.PosPage());
            NavPos.IsChecked = true;
        };
    }

    private void NavigateTo(Page page)
    {
        MainFrame.Navigate(page);
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
        => NavigateTo(new Pages.DashboardPage());

    private void NavPos_Click(object sender, RoutedEventArgs e)
        => NavigateTo(new Pages.PosPage());

    private void NavOrders_Click(object sender, RoutedEventArgs e)
        => NavigateTo(new Pages.OrdersPage());

    private void NavProducts_Click(object sender, RoutedEventArgs e)
        => NavigateTo(new Pages.ProductsPage());

    private void NavSettings_Click(object sender, RoutedEventArgs e)
        => NavigateTo(new Pages.SettingsPage());

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var confirm = System.Windows.MessageBox.Show(
            "Xác nhận kết ca và đăng xuất khỏi trạm POS thu ngân?",
            "Kết ca làm việc",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (confirm == System.Windows.MessageBoxResult.Yes)
        {
            var login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            this.Close();
        }
    }

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private bool _isSidebarCollapsed = false;

    private void BtnCollapseSidebar_Click(object sender, RoutedEventArgs e)
    {
        SetSidebarCollapsed(true);
    }

    private void BtnExpandSidebar_Click(object sender, RoutedEventArgs e)
    {
        SetSidebarCollapsed(false);
    }

    private void SetSidebarCollapsed(bool collapsed)
    {
        _isSidebarCollapsed = collapsed;
        if (_isSidebarCollapsed)
        {
            ColSidebar.Width = new GridLength(68);
            if (PnlSidebarExpandedHeader != null) PnlSidebarExpandedHeader.Visibility = Visibility.Collapsed;
            if (PnlSidebarCollapsedHeader != null)
            {
                PnlSidebarCollapsedHeader.Visibility = Visibility.Visible;
                if (ImgMiniLogo != null) ImgMiniLogo.Visibility = Visibility.Visible;
                if (BorderExpandIcon != null) BorderExpandIcon.Visibility = Visibility.Collapsed;
            }
            if (TxtLogoutLabel != null) TxtLogoutLabel.Visibility = Visibility.Collapsed;
        }
        else
        {
            ColSidebar.Width = new GridLength(240);
            if (PnlSidebarExpandedHeader != null) PnlSidebarExpandedHeader.Visibility = Visibility.Visible;
            if (PnlSidebarCollapsedHeader != null) PnlSidebarCollapsedHeader.Visibility = Visibility.Collapsed;
            if (TxtLogoutLabel != null) TxtLogoutLabel.Visibility = Visibility.Visible;
        }
    }

    private void PnlSidebarCollapsedHeader_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (ImgMiniLogo != null) ImgMiniLogo.Visibility = Visibility.Collapsed;
        if (BorderExpandIcon != null) BorderExpandIcon.Visibility = Visibility.Visible;
    }

    private void PnlSidebarCollapsedHeader_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (ImgMiniLogo != null) ImgMiniLogo.Visibility = Visibility.Visible;
        if (BorderExpandIcon != null) BorderExpandIcon.Visibility = Visibility.Collapsed;
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void BtnWindowClose_Click(object sender, RoutedEventArgs e)
        => Close();
}
