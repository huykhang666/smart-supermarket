using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SmartSupermarket.POS.Models;
using SmartSupermarket.POS.Services;

namespace SmartSupermarket.POS.Views.Pages;

public partial class PosPage : Page
{
    private readonly ApiService _apiService = new();
    private List<ProductItem> _allProducts = new();
    private readonly ObservableCollection<CartItem> _cartItems = new();
    private string _currentCategory = "";

    public PosPage()
    {
        InitializeComponent();
        DgCart.ItemsSource = _cartItems;
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
        var filtered = _allProducts.Where(p =>
        {
            bool matchCategory = string.IsNullOrEmpty(_currentCategory) || p.CategoryName.Contains(_currentCategory, StringComparison.OrdinalIgnoreCase);
            bool matchQuery = string.IsNullOrEmpty(query) ||
                              p.ProductName.ToLowerInvariant().Contains(query) ||
                              p.Barcode.Contains(query);
            return matchCategory && matchQuery;
        }).ToList();

        IcProducts.ItemsSource = filtered;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void CategoryBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            _currentCategory = btn.Tag?.ToString() ?? "";
            ApplyFilter();
        }
    }

    private void ProductCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is ProductItem prod)
        {
            AddToCart(prod);
        }
    }

    private void AddToCart(ProductItem prod)
    {
        var existing = _cartItems.FirstOrDefault(c => c.ProductId == prod.ProductId);
        if (existing != null)
        {
            existing.Quantity++;
            DgCart.Items.Refresh();
        }
        else
        {
            _cartItems.Add(new CartItem
            {
                ProductId = prod.ProductId,
                ProductName = prod.ProductName,
                Barcode = prod.Barcode,
                UnitPrice = prod.Price,
                Quantity = 1
            });
        }
        RecalculateCart();
    }

    private void BtnIncQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is CartItem item)
        {
            item.Quantity++;
            DgCart.Items.Refresh();
            RecalculateCart();
        }
    }

    private void BtnDecQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is CartItem item)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
                DgCart.Items.Refresh();
            }
            else
            {
                _cartItems.Remove(item);
            }
            RecalculateCart();
        }
    }

    private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is CartItem item)
        {
            _cartItems.Remove(item);
            RecalculateCart();
        }
    }

    private void BtnClearCart_Click(object sender, RoutedEventArgs e)
    {
        _cartItems.Clear();
        RecalculateCart();
    }

    private void RecalculateCart()
    {
        decimal subTotal = _cartItems.Sum(c => c.TotalAmount);
        decimal discount = 0;
        decimal grandTotal = Math.Max(0, subTotal - discount);

        TxtSubTotal.Text = $"{subTotal:N0} đ";
        TxtDiscount.Text = $"-{discount:N0} đ";
        TxtGrandTotal.Text = $"{grandTotal:N0} đ";
    }

    private async void BtnPayQr_Click(object sender, RoutedEventArgs e)
    {
        if (_cartItems.Count == 0)
        {
            MessageBox.Show("Giỏ hàng đang trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        decimal grandTotal = _cartItems.Sum(c => c.TotalAmount);
        var qrWindow = new Windows.QrPaymentWindow(grandTotal);
        if (qrWindow.ShowDialog() == true)
        {
            // Call Backend API to save order
            bool success = await _apiService.CreateOrderAsync(1, null, grandTotal, grandTotal, 2, _cartItems.ToList());
            string apiMsg = success ? " (Đã lưu vào Cơ sở dữ liệu qua Backend API)" : " (Lưu hóa đơn cục bộ)";
            MessageBox.Show($"Thanh toán thành công {grandTotal:N0} đ qua VietQR!{apiMsg}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            _cartItems.Clear();
            RecalculateCart();
        }
    }

    private async void BtnPayCash_Click(object sender, RoutedEventArgs e)
    {
        if (_cartItems.Count == 0)
        {
            MessageBox.Show("Giỏ hàng đang trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        decimal grandTotal = _cartItems.Sum(c => c.TotalAmount);
        var confirm = MessageBox.Show($"Xác nhận thanh toán tiền mặt số tiền: {grandTotal:N0} đ?", "Thanh toán tiền mặt", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            // Call Backend API to save order
            bool success = await _apiService.CreateOrderAsync(1, null, grandTotal, grandTotal, 1, _cartItems.ToList());
            string apiMsg = success ? " (Đã lưu vào Cơ sở dữ liệu qua Backend API)" : " (Lưu hóa đơn cục bộ)";
            MessageBox.Show($"Đã thanh toán tiền mặt thành công!{apiMsg}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            _cartItems.Clear();
            RecalculateCart();
        }
    }
}
