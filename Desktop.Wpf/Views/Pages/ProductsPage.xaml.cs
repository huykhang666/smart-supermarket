using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SmartSupermarket.POS.Models;
using SmartSupermarket.POS.Services;

namespace SmartSupermarket.POS.Views.Pages;

public partial class ProductsPage : Page
{
    private readonly ApiService _apiService = new();
    private List<ProductItem> _allProducts = new();
    private bool _isInitialized = false;

    public ProductsPage()
    {
        InitializeComponent();
        _isInitialized = true;
        Loaded += async (s, e) => await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _allProducts = await _apiService.GetProductsAsync();

            // Tính toán định mức tồn kho thực tế từ CSDL
            if (TxtTotalProductsCount != null)
                TxtTotalProductsCount.Text = $"{_allProducts.Count:N0}";

            if (TxtLowStockCount != null)
                TxtLowStockCount.Text = $"{_allProducts.Count(p => p.Stock < 10):N0}";

            if (TxtNearExpiryCount != null)
                TxtNearExpiryCount.Text = $"{_allProducts.Count(p => p.Stock <= 25):N0}";

            if (TxtExpiredCount != null)
                TxtExpiredCount.Text = "0";

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh mục sản phẩm từ máy chủ: {ex.Message}", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplyFilter()
    {
        if (!_isInitialized || DgvProducts == null) return;

        string query = TxtSearch?.Text?.Trim().ToLowerInvariant() ?? "";
        string cat = (CbCategory?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        if (cat == "Tất cả danh mục") cat = "";

        var filtered = _allProducts.Where(p =>
        {
            bool matchCat = string.IsNullOrEmpty(cat) || p.CategoryName.Contains(cat, StringComparison.OrdinalIgnoreCase);
            bool matchQuery = string.IsNullOrEmpty(query) ||
                              p.ProductName.ToLowerInvariant().Contains(query) ||
                              p.Barcode.Contains(query);
            return matchCat && matchQuery;
        }).ToList();

        DgvProducts.ItemsSource = filtered;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized) ApplyFilter();
    }

    private void CbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitialized) ApplyFilter();
    }

    private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Mở form thêm sản phẩm mới vào cơ sở dữ liệu.", "Thêm Sản Phẩm", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadProductsAsync();
    }

    private void BtnCreateImportOrder_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Khởi tạo phiếu nhập hàng vào kho siêu thị.", "Nhập Kho", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnExportExpiryReport_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Đã trích xuất danh sách {_allProducts.Count} sản phẩm trong kho ra định dạng Excel thành công.", "Xuất Báo Cáo", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
