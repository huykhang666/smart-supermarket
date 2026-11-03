using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using SmartSupermarket.POS.Services;
using Wpf.Ui.Controls;

namespace SmartSupermarket.POS.Views.Windows;

public partial class LoginWindow : FluentWindow
{
    private readonly AuthService _authService = new();

    private readonly SolidColorBrush _defaultBorderBrush = new((Color)ColorConverter.ConvertFromString("#334155"));
    private readonly SolidColorBrush _activeBorderBrush = new((Color)ColorConverter.ConvertFromString("#38BDF8"));
    private readonly SolidColorBrush _defaultBgBrush = new((Color)ColorConverter.ConvertFromString("#251E293B"));
    private readonly SolidColorBrush _activeBgBrush = new((Color)ColorConverter.ConvertFromString("#351E293B"));

    private string _pendingUsername = "";
    private string _pendingPassword = "";
    private string _enteredPin = "";

    private void SetLoginStatus(string message, Brush? brush = null)
    {
        if (TxtLoginStatus == null) return;
        TxtLoginStatus.Text = message;
        TxtLoginStatus.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
        if (brush != null)
        {
            TxtLoginStatus.Foreground = brush;
        }
    }

    private void SetPinStatus(string message, Brush? brush = null)
    {
        if (TxtPinStatus == null) return;
        TxtPinStatus.Text = message;
        TxtPinStatus.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
        if (brush != null)
        {
            TxtPinStatus.Foreground = brush;
        }
    }


    public LoginWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    // Dynamic Focus Effects for input fields
    private void InputBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (BorderUsername != null)
        {
            BorderUsername.BorderBrush = _activeBorderBrush;
            BorderUsername.Background = _activeBgBrush;
            BorderUsername.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#0284C7"),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.4
            };
        }
    }

    private void InputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (BorderUsername != null)
        {
            BorderUsername.BorderBrush = _defaultBorderBrush;
            BorderUsername.Background = _defaultBgBrush;
            BorderUsername.Effect = null;
        }
    }

    private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (BorderPassword != null)
        {
            BorderPassword.BorderBrush = _activeBorderBrush;
            BorderPassword.Background = _activeBgBrush;
            BorderPassword.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#0284C7"),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.4
            };
        }
    }

    private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (BorderPassword != null)
        {
            BorderPassword.BorderBrush = _defaultBorderBrush;
            BorderPassword.Background = _defaultBgBrush;
            BorderPassword.Effect = null;
        }
    }

    private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PlaceholderUsername != null)
        {
            PlaceholderUsername.Visibility = string.IsNullOrEmpty(TxtUsername.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (PlaceholderPassword != null)
        {
            PlaceholderPassword.Visibility = string.IsNullOrEmpty(TxtPassword.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void LinkForgotPassword_Click(object sender, MouseButtonEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Vui lòng liên hệ Quản trị viên hệ thống (IT Admin) qua nội bộ để đặt lại mật khẩu trạm POS.",
            "Quên mật khẩu",
            System.Windows.MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void LinkNeedHelp_Click(object sender, MouseButtonEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Smart Supermarket POS Workstation v2.0\n" +
            "Hỗ trợ kỹ thuật: support@smartsupermarket.local\n" +
            "Hotline hỗ trợ ca: 1900-8888\n\n" +
            "Tài khoản mẫu:\n" +
            "• Quản trị viên: admin / admin123 (PIN: 123456)\n" +
            "• Thu ngân: staff / staff123 hoặc nhanvien / nhanvien123 (PIN: 123456)",
            "Trợ giúp hệ thống",
            System.Windows.MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // ─── BƯỚC 1: XÁC THỰC TÀI KHOẢN & MẬT KHẨU ───
    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string username = TxtUsername.Text?.Trim() ?? "";
        string password = TxtPassword.Password ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetLoginStatus("Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu.", System.Windows.Media.Brushes.OrangeRed);
            return;
        }

        BtnLogin.IsEnabled = false;
        SetLoginStatus("Đang kiểm tra tài khoản...", System.Windows.Media.Brushes.LightSkyBlue);

        await Task.Delay(200);

        // Kiểm tra sơ bộ username / password
        bool isValidAccount = (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123") ||
                              (username.Equals("staff", StringComparison.OrdinalIgnoreCase) && password == "staff123") ||
                              (username.Equals("nhanvien", StringComparison.OrdinalIgnoreCase) && password == "nhanvien123");

        BtnLogin.IsEnabled = true;

        if (isValidAccount)
        {
            _pendingUsername = username;
            _pendingPassword = password;

            // Chuyển sang Bước 2: Nhập Mã PIN
            PnlStepCredentials.Visibility = Visibility.Collapsed;
            PnlStepPin.Visibility = Visibility.Visible;
            _enteredPin = "";
            UpdatePinDisplay();
            SetLoginStatus("");
            SetPinStatus("");
            this.Focus();
        }
        else
        {
            SetLoginStatus("✕ Tên đăng nhập hoặc mật khẩu không chính xác.", System.Windows.Media.Brushes.Salmon);
        }
    }

    // ─── BƯỚC 2: BÀN PHÍM MÃ PIN (NUMPAD) ───
    private void BtnPinNum_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string digit)
        {
            AddPinDigit(digit);
        }
    }

    private void BtnPinDel_Click(object sender, RoutedEventArgs e)
    {
        DeletePinDigit();
    }

    private void BtnPinBack_Click(object sender, RoutedEventArgs e)
    {
        PnlStepPin.Visibility = Visibility.Collapsed;
        PnlStepCredentials.Visibility = Visibility.Visible;
        _enteredPin = "";
        SetPinStatus("");
        SetLoginStatus("");
    }

    private void BtnConfirmPin_Click(object sender, RoutedEventArgs e)
    {
        ExecutePinLogin();
    }

    private void AddPinDigit(string digit)
    {
        if (_enteredPin.Length < 6)
        {
            _enteredPin += digit;
            UpdatePinDisplay();
            SetPinStatus("");
        }
    }

    private void DeletePinDigit()
    {
        if (_enteredPin.Length > 0)
        {
            _enteredPin = _enteredPin.Substring(0, _enteredPin.Length - 1);
            UpdatePinDisplay();
            SetPinStatus("");
        }
    }

    private void UpdatePinDisplay()
    {
        System.Windows.Controls.TextBlock[] digits = { PinDigit0, PinDigit1, PinDigit2, PinDigit3, PinDigit4, PinDigit5 };
        Border[] boxes = { BoxPin0, BoxPin1, BoxPin2, BoxPin3, BoxPin4, BoxPin5 };

        for (int i = 0; i < 6; i++)
        {
            if (i < _enteredPin.Length)
            {
                digits[i].Text = "●";
                boxes[i].BorderBrush = _activeBorderBrush;
                boxes[i].Background = _activeBgBrush;
            }
            else
            {
                digits[i].Text = "";
                boxes[i].BorderBrush = _defaultBorderBrush;
                boxes[i].Background = _defaultBgBrush;
            }
        }
    }

    private async void ExecutePinLogin()
    {
        if (_enteredPin.Length < 4)
        {
            SetPinStatus("Mã PIN phải từ 4 đến 6 chữ số.", System.Windows.Media.Brushes.Salmon);
            return;
        }

        BtnConfirmPin.IsEnabled = false;
        SetPinStatus("Đang xác thực mã PIN...", System.Windows.Media.Brushes.LightSkyBlue);

        try
        {
            var result = await _authService.LoginAsync(_pendingUsername, _pendingPassword, _enteredPin);

            if (result.Success)
            {
                SetPinStatus("Đăng nhập thành công ✓", new SolidColorBrush(Color.FromRgb(74, 222, 128)));

                await Task.Delay(350);

                if (result.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                    result.Role.Equals("Cashier", StringComparison.OrdinalIgnoreCase) ||
                    result.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    var empWin = new EmployeeMainWindow();
                    Application.Current.MainWindow = empWin;
                    empWin.Show();
                    this.Close();
                }
                else
                {
                    var adminWin = new AdminMainWindow();
                    Application.Current.MainWindow = adminWin;
                    adminWin.Show();
                    this.Close();
                }
            }
            else
            {
                SetPinStatus("✕ Mã PIN không chính xác (Mặc định: 123456)", System.Windows.Media.Brushes.Salmon);
                _enteredPin = "";
                UpdatePinDisplay();
                BtnConfirmPin.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Lỗi khởi động cửa sổ chính: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            BtnConfirmPin.IsEnabled = true;
        }
    }

    // Hỗ trợ phím số vật lý trên bàn phím máy tính
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (PnlStepPin.Visibility == Visibility.Visible)
        {
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                int val = e.Key - Key.D0;
                AddPinDigit(val.ToString());
                e.Handled = true;
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                int val = e.Key - Key.NumPad0;
                AddPinDigit(val.ToString());
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                DeletePinDigit();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ExecutePinLogin();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                BtnPinBack_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
        else
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}
