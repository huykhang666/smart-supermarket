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

    public ProductsPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        _allProducts = await _apiService.GetProductsAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string query = TxtSearch.Text?.Trim().ToLowerInvariant() ?? "";
        string cat = (CbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
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

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void CbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Mở form thêm sản phẩm mới vào cơ sở dữ liệu.", "Thêm Sản Phẩm", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
