using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using SmartSupermarket.POS.Models;
using SmartSupermarket.POS.Services;
using SmartSupermarket.POS.Views.Windows;

namespace SmartSupermarket.POS.Views.Pages;

public partial class OrdersPage : Page
{
    private readonly ApiService _apiService = new();
    private List<OrderItem> _allOrders = new();
    private bool _isInitialized = false;

    public OrdersPage()
    {
        InitializeComponent();
        _isInitialized = true;
        Loaded += async (s, e) => await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        _allOrders = await _apiService.GetOrdersAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (!_isInitialized || DgvOrders == null) return;

        string query = TxtSearchOrder?.Text?.Trim().ToLowerInvariant() ?? "";
        string status = (CbStatusFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        if (status == "Tất cả trạng thái") status = "";

        var filtered = _allOrders.Where(o =>
        {
            bool matchQuery = string.IsNullOrEmpty(query) ||
                              o.OrderCode.ToLowerInvariant().Contains(query) ||
                              o.CustomerName.ToLowerInvariant().Contains(query) ||
                              o.EmployeeName.ToLowerInvariant().Contains(query) ||
                              o.PaymentMethod.ToLowerInvariant().Contains(query);

            bool matchStatus = string.IsNullOrEmpty(status) || o.Status.Contains(status, StringComparison.OrdinalIgnoreCase);

            return matchQuery && matchStatus;
        }).ToList();

        DgvOrders.ItemsSource = filtered;

        // Update footer summary
        if (TxtOrderCountSummary != null)
        {
            TxtOrderCountSummary.Text = $"Hiển thị: {filtered.Count} / {_allOrders.Count} đơn hàng";
        }

        if (TxtTotalRevenueSummary != null)
        {
            decimal sum = filtered.Sum(x => x.FinalAmount);
            TxtTotalRevenueSummary.Text = $"Tổng thực thu: {sum:N0} đ";
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadOrdersAsync();
    }

    private void TxtSearchOrder_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized) ApplyFilter();
    }

    private void CbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitialized) ApplyFilter();
    }

    private void BtnFilterToday_Click(object sender, RoutedEventArgs e)
    {
        DateTime today = DateTime.Today;
        var filtered = _allOrders.Where(o => o.OrderDate.Date == today).ToList();
        DgvOrders.ItemsSource = filtered;
        UpdateSummary(filtered);
    }

    private void BtnFilterWeek_Click(object sender, RoutedEventArgs e)
    {
        DateTime pastWeek = DateTime.Today.AddDays(-7);
        var filtered = _allOrders.Where(o => o.OrderDate.Date >= pastWeek).ToList();
        DgvOrders.ItemsSource = filtered;
        UpdateSummary(filtered);
    }

    private void BtnFilterAll_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void UpdateSummary(List<OrderItem> list)
    {
        if (TxtOrderCountSummary != null)
            TxtOrderCountSummary.Text = $"Hiển thị: {list.Count} / {_allOrders.Count} đơn hàng";
        if (TxtTotalRevenueSummary != null)
            TxtTotalRevenueSummary.Text = $"Tổng thực thu: {list.Sum(x => x.FinalAmount):N0} đ";
    }

    private void BtnViewDetail_Click(object sender, RoutedEventArgs e)
    {
        ShowSelectedOrderReceipt();
    }

    private void BtnPrintReceipt_Click(object sender, RoutedEventArgs e)
    {
        ShowSelectedOrderReceipt();
    }

    private void DgvOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ShowSelectedOrderReceipt();
    }

    private void ShowSelectedOrderReceipt()
    {
        if (DgvOrders.SelectedItem is not OrderItem order)
        {
            MessageBox.Show("Vui lòng chọn một đơn hàng từ bảng để xem chi tiết / in hóa đơn.",
                            "Chưa chọn đơn hàng", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var cartItems = order.Items.Any()
            ? order.Items
            : new List<CartItem>
            {
                new() { ProductName = "Sản phẩm theo đơn " + order.OrderCode, Quantity = 1, UnitPrice = order.FinalAmount }
            };

        var receiptWin = new ReceiptPreviewWindow(order.OrderCode, order.CustomerName, order.PaymentMethod, order.TotalAmount, order.DiscountAmount, order.FinalAmount, 0, cartItems)
        {
            Owner = Window.GetWindow(this)
        };
        receiptWin.ShowDialog();
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var items = DgvOrders.ItemsSource as List<OrderItem> ?? _allOrders;
        if (!items.Any())
        {
            MessageBox.Show("Không có dữ liệu đơn hàng để xuất file.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV File (*.csv)|*.csv",
            FileName = $"BaoCao_DonHang_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Mã Đơn,Khách Hàng,Thu Ngân,Thời Gian,Phương Thức,Tổng Tiền,Trạng Thái");
                foreach (var o in items)
                {
                    sb.AppendLine($"\"{o.OrderCode}\",\"{o.CustomerName}\",\"{o.EmployeeName}\",\"{o.FormattedDate}\",\"{o.PaymentMethod}\",\"{o.FinalAmount}\",\"{o.Status}\"");
                }
                File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Đã xuất thành công {items.Count} đơn hàng ra file:\n{saveDialog.FileName}", "Xuất file thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
    {
        if (DgvOrders.SelectedItem is not OrderItem order)
        {
            MessageBox.Show("Vui lòng chọn đơn hàng cần hủy.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (order.Status == "Đã hủy")
        {
            MessageBox.Show("Đơn hàng này đã ở trạng thái Đã hủy trước đó.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Bạn có chắc chắn muốn hủy đơn hàng {order.OrderCode} không?",
                                      "Xác nhận hủy đơn", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            order.Status = "Đã hủy";
            DgvOrders.Items.Refresh();
            MessageBox.Show($"Đã hủy thành công đơn hàng {order.OrderCode}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
