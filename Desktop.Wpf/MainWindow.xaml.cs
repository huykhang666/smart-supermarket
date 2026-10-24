using System.Windows;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            RootNavigation.Navigate(typeof(Views.Pages.DashboardPage));
        };
    }
}
