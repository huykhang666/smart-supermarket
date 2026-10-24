using System.Windows;
using System.Windows.Controls;
using SmartSupermarket.POS.Services;

namespace SmartSupermarket.POS.Views.Pages;

public partial class DashboardPage : Page
{
    private readonly ApiService _apiService = new();

    public DashboardPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        var orders = await _apiService.GetOrdersAsync();
        DgvRecentOrders.ItemsSource = orders;

        decimal totalRev = 0;
        foreach (var o in orders)
        {
            totalRev += o.FinalAmount;
        }

        TxtTodayRevenue.Text = $"{totalRev:N0} đ";
        TxtTodayOrders.Text = $"{orders.Count} đơn";
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardDataAsync();
    }
}
