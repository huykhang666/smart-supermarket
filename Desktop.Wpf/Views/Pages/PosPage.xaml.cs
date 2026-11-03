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
    private int? _currentCustomerId = null;
    private bool _isInitialized = false;

    public PosPage()
    {
        InitializeComponent();
        _isInitialized = true;
        DgCart.ItemsSource = _cartItems;
        UpdateBilling();
        Loaded += async (s, e) => await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        _allProducts = await _apiService.GetProductsAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (!_isInitialized || IcProducts == null) return;

        string query = TxtSearch?.Text?.Trim().ToLowerInvariant() ?? "";
        string cat = (CbCategoryFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? _currentCategory;
        if (cat == "ALL") cat = "";

        var filtered = _allProducts.Where(p =>
        {
            bool matchCategory = string.IsNullOrEmpty(cat) || p.CategoryName.Contains(cat, StringComparison.OrdinalIgnoreCase);
            bool matchQuery = string.IsNullOrEmpty(query) ||
                              p.ProductName.ToLowerInvariant().Contains(query) ||
                              p.Barcode.Contains(query);
            return matchCategory && matchQuery;
        }).ToList();

        IcProducts.ItemsSource = filtered;

        if (TxtProductCount != null)
        {
            TxtProductCount.Text = $"{filtered.Count} sản phẩm";
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized) ApplyFilter();
    }

    private void CbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        var tag = (CbCategoryFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
        _currentCategory = tag == "ALL" ? "" : tag;
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
        UpdateBilling();
    }

    private void BtnIncQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is CartItem item)
        {
            item.Quantity++;
            DgCart.Items.Refresh();
            UpdateBilling();
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
            UpdateBilling();
        }
    }

    private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is CartItem item)
        {
            _cartItems.Remove(item);
            UpdateBilling();
        }
    }

    private void BtnClearCart_Click(object sender, RoutedEventArgs e)
    {
        if (_cartItems.Any())
        {
            var res = MessageBox.Show("Bạn có chắc chắn muốn xóa toàn bộ giỏ hàng?", "Xóa Giỏ Hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _cartItems.Clear();
                _discountAmount = 0;
                TxtVoucherCode.Text = "";
                UpdateBilling();
            }
        }
    }

    private void UpdateBilling()
    {
        decimal subTotal = _cartItems.Sum(x => x.TotalAmount);
        decimal vat = Math.Round(subTotal * 0.08m, 0);
        decimal grandTotal = Math.Max(0, subTotal + vat - _discountAmount);

        if (TxtSubTotal != null) TxtSubTotal.Text = $"{subTotal:N0} đ";
        if (TxtVat != null) TxtVat.Text = $"{vat:N0} đ";
        if (TxtDiscount != null) TxtDiscount.Text = $"-{_discountAmount:N0} đ";
        if (TxtGrandTotal != null) TxtGrandTotal.Text = $"{grandTotal:N0} đ";
        if (TxtCartItemCount != null) TxtCartItemCount.Text = $"({_cartItems.Sum(x => x.Quantity)})";
        if (PnlCartEmpty != null) PnlCartEmpty.Visibility = _cartItems.Any() ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BtnApplyVoucher_Click(object sender, RoutedEventArgs e)
    {
        string code = TxtVoucherCode.Text?.Trim().ToUpperInvariant() ?? "";
        if (code == "GIAM10" || code == "SALE10")
        {
            decimal subTotal = _cartItems.Sum(x => x.TotalAmount);
            _discountAmount = Math.Round(subTotal * 0.1m, 0);
            UpdateBilling();
            MessageBox.Show($"Áp dụng Voucher thành công! Giảm: {_discountAmount:N0} đ (10%)", "Voucher Hợp Lệ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (code == "KATQ20K")
        {
            _discountAmount = 20000;
            UpdateBilling();
            MessageBox.Show("Áp dụng Voucher KATQ20K thành công! Giảm ngay: 20.000 đ", "Voucher Hợp Lệ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (string.IsNullOrEmpty(code))
        {
            _discountAmount = 0;
            UpdateBilling();
        }
        else
        {
            MessageBox.Show("Mã voucher không hợp lệ hoặc đã hết hạn!", "Không Hợp Lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void TxtCustomerPhone_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized || TxtCustomerPhone == null || TxtCustomerLoyaltyInfo == null) return;

        string phone = TxtCustomerPhone.Text?.Trim() ?? "";
        if (phone.Length >= 9)
        {
            var customer = await _apiService.LookupCustomerAsync(phone);
            if (customer != null)
            {
                _currentCustomerId = customer.CustomerId;
                _customerName = customer.FullName;
                TxtCustomerLoyaltyInfo.Text = $"{customer.Tier}: {_customerName} • {customer.LoyaltyPoints}đ";
                TxtCustomerLoyaltyInfo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 132, 199)); // #0284C7
                if (BorderCustomerBadge != null)
                {
                    BorderCustomerBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255)); // #F0F9FF
                    BorderCustomerBadge.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(186, 230, 253)); // #BAE6FD
                }
                if (IconCustomerBadge != null)
                {
                    IconCustomerBadge.Symbol = Wpf.Ui.Controls.SymbolRegular.PersonStar20;
                    IconCustomerBadge.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 132, 199));
                }
                return;
            }
        }

        _currentCustomerId = null;
        _customerName = "Khách Lẻ";
        TxtCustomerLoyaltyInfo.Text = "Khách Lẻ";
        TxtCustomerLoyaltyInfo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)); // #334155
        if (BorderCustomerBadge != null)
        {
            BorderCustomerBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
            BorderCustomerBadge.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)); // #CBD5E1
        }
        if (IconCustomerBadge != null)
        {
            IconCustomerBadge.Symbol = Wpf.Ui.Controls.SymbolRegular.Person20;
            IconCustomerBadge.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
        }
    }

    private void BtnParkOrder_Click(object sender, RoutedEventArgs e)
    {
        if (!_cartItems.Any())
        {
            MessageBox.Show("Giỏ hàng đang trống, không thể lưu đơn tạm!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _parkedOrders.Add(new List<CartItem>(_cartItems));
        _cartItems.Clear();
        UpdateBilling();
        if (TxtParkedCount != null) TxtParkedCount.Text = $"Đơn tạm ({_parkedOrders.Count})";
        else BtnResumeParked.Content = $"Đơn tạm ({_parkedOrders.Count})";
        MessageBox.Show($"Đã lưu đơn tạm thành công! Tổng số đơn tạm: {_parkedOrders.Count}", "Lưu Đơn Tạm", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnResumeParked_Click(object sender, RoutedEventArgs e)
    {
        if (!_parkedOrders.Any())
        {
            MessageBox.Show("Không có đơn hàng tạm nào đang chờ!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var lastOrder = _parkedOrders.Last();
        _parkedOrders.RemoveAt(_parkedOrders.Count - 1);
        _cartItems.Clear();
        foreach (var item in lastOrder)
        {
            _cartItems.Add(item);
        }
        UpdateBilling();
        if (TxtParkedCount != null) TxtParkedCount.Text = $"Đơn tạm ({_parkedOrders.Count})";
        else BtnResumeParked.Content = $"Đơn tạm ({_parkedOrders.Count})";
        MessageBox.Show("Đã nạp lại đơn tạm thành công!", "Nạp Đơn", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnPayCash_Click(object sender, RoutedEventArgs e)
    {
        if (!_cartItems.Any())
        {
            MessageBox.Show("Giỏ hàng đang trống, vui lòng chọn sản phẩm!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal subTotal = _cartItems.Sum(x => x.TotalAmount);
        decimal vat = Math.Round(subTotal * 0.08m, 0);
        decimal grandTotal = Math.Max(0, subTotal + vat - _discountAmount);

        var cashDialog = new CashPaymentDialog(grandTotal)
        {
            Owner = Window.GetWindow(this)
        };
        if (cashDialog.ShowDialog() == true)
        {
            CompleteCheckout("Tiền mặt", subTotal, _discountAmount, grandTotal, cashDialog.ChangeDue);
        }
    }

    private void BtnPayQr_Click(object sender, RoutedEventArgs e)
    {
        if (!_cartItems.Any())
        {
            MessageBox.Show("Giỏ hàng đang trống, vui lòng chọn sản phẩm!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal subTotal = _cartItems.Sum(x => x.TotalAmount);
        decimal vat = Math.Round(subTotal * 0.08m, 0);
        decimal grandTotal = Math.Max(0, subTotal + vat - _discountAmount);

        var qrDialog = new QrPaymentWindow(grandTotal)
        {
            Owner = Window.GetWindow(this)
        };
        if (qrDialog.ShowDialog() == true)
        {
            CompleteCheckout("VietQR", subTotal, _discountAmount, grandTotal, 0);
        }
    }

    private async void CompleteCheckout(string paymentMethod, decimal subTotal, decimal discount, decimal grandTotal, decimal changeDue)
    {
        string orderCode = $"ORD-{new Random().Next(10000, 99999)}";
        var snapshotItems = new List<CartItem>(_cartItems);

        int payMethodCode = paymentMethod == "VietQR" ? 2 : 1;
        await _apiService.CreateOrderAsync(1, _currentCustomerId, subTotal, grandTotal, payMethodCode, snapshotItems);
        _currentCustomerId = null;

        _cartItems.Clear();
        _discountAmount = 0;
        TxtVoucherCode.Text = "";
        UpdateBilling();

        var receiptWindow = new ReceiptPreviewWindow(orderCode, _customerName, paymentMethod, subTotal, discount, grandTotal, changeDue, snapshotItems)
        {
            Owner = Window.GetWindow(this)
        };
        receiptWindow.ShowDialog();
    }
}
