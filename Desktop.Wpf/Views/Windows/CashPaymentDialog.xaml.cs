using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS.Views.Windows;

public partial class CashPaymentDialog : FluentWindow
{
    public decimal TotalPayable { get; }
    public decimal CashGiven { get; private set; }
    public decimal ChangeDue { get; private set; }

    public CashPaymentDialog(decimal totalAmount)
    {
        InitializeComponent();
        TotalPayable = totalAmount;
        TxtTotalAmount.Text = $"{TotalPayable:N0} đ";
        TxtCashGiven.Text = $"{TotalPayable:N0}";
        TxtCashGiven.Focus();
        TxtCashGiven.SelectAll();
        CalculateChange();
    }

    private void TxtCashGiven_TextChanged(object sender, TextChangedEventArgs e)
    {
        CalculateChange();
    }

    private void CalculateChange()
    {
        string raw = (TxtCashGiven.Text ?? "").Replace(",", "").Replace(".", "").Replace("đ", "").Trim();
        if (decimal.TryParse(raw, out decimal given))
        {
            CashGiven = given;
            ChangeDue = Math.Max(0, CashGiven - TotalPayable);
            TxtChangeDue.Text = $"{ChangeDue:N0} đ";
            BtnConfirm.IsEnabled = CashGiven >= TotalPayable;
        }
        else
        {
            CashGiven = 0;
            ChangeDue = 0;
            TxtChangeDue.Text = "0 đ";
            BtnConfirm.IsEnabled = false;
        }
    }

    private void QuickCash_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
        {
            if (tag == "EXACT")
            {
                TxtCashGiven.Text = $"{TotalPayable:N0}";
            }
            else if (decimal.TryParse(tag, out decimal val))
            {
                TxtCashGiven.Text = $"{val:N0}";
            }
        }
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (CashGiven < TotalPayable)
        {
            System.Windows.MessageBox.Show("Số tiền khách đưa chưa đủ để thanh toán đơn hàng!", "Cảnh báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
