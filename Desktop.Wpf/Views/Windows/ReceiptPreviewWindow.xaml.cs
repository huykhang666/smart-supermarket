using System;
using System.Collections.Generic;
using System.Windows;
using SmartSupermarket.POS.Models;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS.Views.Windows;

public partial class ReceiptPreviewWindow : FluentWindow
{
    public ReceiptPreviewWindow(string orderCode, string customerName, string paymentMethod,
                                decimal subTotal, decimal discount, decimal grandTotal,
                                decimal changeDue, List<CartItem> items)
    {
        InitializeComponent();

        TxtOrderCode.Text = orderCode;
        TxtOrderTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        TxtCustomer.Text = customerName;
        TxtPaymentMethod.Text = paymentMethod;

        decimal vat = Math.Round(grandTotal - (grandTotal / 1.08m), 0);
        TxtSubTotal.Text = $"{subTotal:N0} đ";
        TxtVat.Text = $"{vat:N0} đ";
        TxtDiscount.Text = $"-{discount:N0} đ";
        TxtGrandTotal.Text = $"{grandTotal:N0} đ";
        TxtChangeDue.Text = $"{changeDue:N0} đ";

        IcReceiptItems.ItemsSource = items;
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Đã gửi lệnh in thành công đến máy in nhiệt hóa đơn K80!", "In Hóa Đơn", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
