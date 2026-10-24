using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS.Views.Windows;

public partial class QrPaymentWindow : FluentWindow
{
    public QrPaymentWindow(decimal amount)
    {
        InitializeComponent();
        TxtAmount.Text = $"{amount:N0} đ";

        try
        {
            // Standard VietQR QuickLink (MBBank demo)
            string qrUrl = $"https://api.vietqr.io/image/970422-0987654321-compact2.jpg?amount={(long)amount}&addInfo=POS%20KATQ%20THANH%20TOAN";
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(qrUrl, UriKind.Absolute);
            bitmap.EndInit();
            ImgQrCode.Source = bitmap;
        }
        catch { }
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
