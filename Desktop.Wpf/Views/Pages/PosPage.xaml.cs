using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SmartSupermarket.POS.Models;
using SmartSupermarket.POS.Services;
using SmartSupermarket.POS.Views.Windows;

namespace SmartSupermarket.POS.Views.Pages;

public partial class PosPage : Page
{
    private readonly ApiService _apiService = new();
    private List<ProductItem> _allProducts = new();
    private readonly ObservableCollection<CartItem> _cartItems = new();
    private readonly List<List<CartItem>> _parkedOrders = new();
    private string _currentCategory = "";
    private decimal _discountAmount = 0;
    private string _customerName = "Khách Lẻ";

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
        if (_cartItems.Count == 0) return;
        var confirm = MessageBox.Show("Xác nhận xóa toàn bộ sản phẩm khỏi giỏ hàng?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            _cartItems.Clear();
            _discountAmount = 0;
            TxtVoucherCode.Text = "";
            RecalculateCart();
        }
    }

    private void TxtCustomerPhone_TextChanged(object sender, TextChangedEventArgs e)
    {
        string phone = (TxtCustomerPhone.Text ?? "").Trim();
        if (phone.Length >= 9)
        {
            _customerName = "Anh Tuấn (09" + phone.Substring(Math.Max(0, phone.Length - 8)) + ")";
            TxtCustomerLoyaltyInfo.Text = $"{_customerName} • 1,450 điểm (Hạng Vàng ⭐)";
        }
        else
        {
            _customerName = "Khách Lẻ";
            TxtCustomerLoyaltyInfo.Text = "Khách Lẻ • Điểm tích lũy: 0 điểm";
        }
    }

    private void BtnApplyVoucher_Click(object sender, RoutedEventArgs e)
    {
        string code = (TxtVoucherCode.Text ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            MessageBox.Show("Vui lòng nhập mã Voucher giảm giá!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        decimal subTotal = _cartItems.Sum(c => c.TotalAmount);
        if (code == "LOYALTY-50K" || code == "50K")
        {
            _discountAmount = 50000;
            MessageBox.Show("Áp dụng thành công Voucher tích điểm giảm 50.000 đ!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (code == "SUPER10" || code == "GIAM10")
        {
            _discountAmount = Math.Round(subTotal * 0.1m, 0);
            MessageBox.Show($"Áp dụng thành công Voucher giảm 10% ({_discountAmount:N0} đ)!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            _discountAmount = 20000;
            MessageBox.Show("Áp dụng mã ưu đãi khai trương giảm 20.000 đ!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        RecalculateCart();
    }

    private void BtnParkOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_cartItems.Count == 0)
        {
            MessageBox.Show("Giỏ hàng đang trống, không thể lưu đơn tạm!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _parkedOrders.Add(_cartItems.ToList());
        _cartItems.Clear();
        _discountAmount = 0;
        TxtVoucherCode.Text = "";
        BtnResumeParked.Content = $"Đơn tạm ({_parkedOrders.Count})";
        RecalculateCart();
        MessageBox.Show("Đã lưu đơn hàng vào danh sách đơn tạm thành công!", "Lưu Đơn Tạm", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnResumeParked_Click(object sender, RoutedEventArgs e)
    {
        if (_parkedOrders.Count == 0)
        {
            MessageBox.Show("Hiện không có đơn hàng tạm nào đang lưu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var lastParked = _parkedOrders[^1];
        _parkedOrders.RemoveAt(_parkedOrders.Count - 1);
        BtnResumeParked.Content = $"Đơn tạm ({_parkedOrders.Count})";

        _cartItems.Clear();
        foreach (var itm in lastParked)
        {
            _cartItems.Add(itm);
        }
        RecalculateCart();
        MessageBox.Show("Đã phục hồi đơn hàng tạm thành công!", "Khôi phục đơn", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RecalculateCart()
    {
        decimal subTotal = _cartItems.Sum(c => c.TotalAmount);
        decimal grandTotal = Math.Max(0, subTotal - _discountAmount);
        decimal vat = Math.Round(grandTotal - (grandTotal / 1.08m), 0);

        TxtSubTotal.Text = $"{subTotal:N0} đ";
        TxtVat.Text = $"{vat:N0} đ";
        TxtDiscount.Text = $"-{_discountAmount:N0} đ";
        TxtGrandTotal.Text = $"{grandTotal:N0} đ";
    }

    private async void BtnPayQr_Click(object sender, RoutedEventArgs e)
    {
        if (_cartItems.Count == 0)
        {
            MessageBox.Show("Giỏ hàng đang trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        decimal subTotal = _cartItems.Sum(c => c.TotalAmount);
        decimal grandTotal = Math.Max(0, subTotal - _discountAmount);

        var qrWindow = new QrPaymentWindow(grandTotal);
        if (qrWindow.ShowDialog() == true)
        {
            // Call Backend API
            await _apiService.CreateOrderAsync(1, null, subTotal, grandTotal, 2, _cartItems.ToList());

            // Open Receipt Preview
            string orderCode = $"ORD-{new Random().Next(1000, 9999):D5}";
            var receipt = new ReceiptPreviewWindow(orderCode, _customerName, "VietQR Banking",
                                                   subTotal, _discountAmount, grandTotal, 0, _cartItems.ToList());
            receipt.ShowDialog();

            _cartItems.Clear();
            _discountAmount = 0;
            TxtVoucherCode.Text = "";
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

        decimal subTotal = _cartItems.Sum(c => c.TotalAmount);
        decimal grandTotal = Math.Max(0, subTotal - _discountAmount);

        var cashDialog = new CashPaymentDialog(grandTotal);
        if (cashDialog.ShowDialog() == true)
        {
            // Call Backend API
            await _apiService.CreateOrderAsync(1, null, subTotal, grandTotal, 1, _cartItems.ToList());

            // Open Receipt Preview
            string orderCode = $"ORD-{new Random().Next(1000, 9999):D5}";
            var receipt = new ReceiptPreviewWindow(orderCode, _customerName, "Tiền Mặt",
                                                   subTotal, _discountAmount, grandTotal, cashDialog.ChangeDue, _cartItems.ToList());
            receipt.ShowDialog();

            _cartItems.Clear();
            _discountAmount = 0;
            TxtVoucherCode.Text = "";
            RecalculateCart();
        }
    }
}
