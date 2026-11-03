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
        try
        {
            // 1. Lấy dữ liệu KPI thống kê và nhận định AI trực tiếp từ Backend
            var summary = await _apiService.GetDashboardSummaryAsync();
            if (summary != null)
            {
                TxtTodayRevenue.Text = summary.Value.TodayRevenue;
                TxtTodayOrders.Text = summary.Value.TodayOrders;
                TxtTotalCustomers.Text = summary.Value.TotalCustomers;
                TxtWarningCount.Text = summary.Value.WarningCount;

                if (!string.IsNullOrWhiteSpace(summary.Value.AiAnalysis))
                {
                    TxtAiAnalysis.Text = summary.Value.AiAnalysis;
                    BorderAiCard.Visibility = Visibility.Visible;
                }
                else
                {
                    BorderAiCard.Visibility = Visibility.Collapsed;
                }
            }

            // 2. Lấy danh sách hóa đơn gần nhất từ CSDL thực
            var orders = await _apiService.GetOrdersAsync();
            DgvRecentOrders.ItemsSource = orders.Take(15).ToList();

            if (summary == null && orders.Count > 0)
            {
                decimal totalRev = orders.Sum(o => o.FinalAmount);
                TxtTodayRevenue.Text = $"{totalRev:N0} đ";
                TxtTodayOrders.Text = $"{orders.Count} đơn";
            }
        }
        catch
        {
            // Dự phòng lỗi mạng
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardDataAsync();
    }
}
