using System.Windows;
using System.Windows.Controls;
using SmartSupermarket.POS.Services;

namespace SmartSupermarket.POS.Views.Pages;

public partial class OrdersPage : Page
{
    private readonly ApiService _apiService = new();

    public OrdersPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        var list = await _apiService.GetOrdersAsync();
        DgvOrders.ItemsSource = list;
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadOrdersAsync();
    }
}
