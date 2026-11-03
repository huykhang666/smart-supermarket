using System;
using System.Windows;

namespace SmartSupermarket.POS;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            MessageBox.Show($"Lỗi hệ thống: {args.ExceptionObject}", "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Lỗi ứng dụng: {args.Exception.Message}\n\nStack:\n{args.Exception.StackTrace}", "Dispatcher Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
