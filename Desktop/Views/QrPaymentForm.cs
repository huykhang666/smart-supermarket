using System;
using System.Drawing;
using System.Media;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class QrPaymentForm : Form
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly int _transactionId;
    private readonly int _orderId;
    private readonly decimal _amount;
    private readonly string _qrData;
    private readonly string _transactionCode;

    private System.Windows.Forms.Timer _pollingTimer = null!;
    private System.Windows.Forms.Timer _countdownTimer = null!;
    private int _remainingSeconds = 900; // 15 phút

    // UI Controls
    private PictureBox picQrCode = null!;
    private Label lblStatusBadge = null!;
    private Label lblTimer = null!;
    private Button btnManualSync = null!;
    private Button btnSimulateSuccess = null!;
    private Button btnCancelPayment = null!;
    private ProgressBar progressBar = null!;

    public QrPaymentForm(HttpClient httpClient, string apiBaseUrl, int transactionId, int orderId, decimal amount, string qrData, string transactionCode)
    {
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;
        _transactionId = transactionId;
        _orderId = orderId;
        _amount = amount;
        _qrData = qrData;
        _transactionCode = transactionCode;

        InitializeComponent();
        LoadQrImage();
        StartTimers();
    }

    private void InitializeComponent()
    {
        this.Text = "Cổng Thanh Toán QR - Smart SuperMarket POS";
        this.Size = new Size(720, 520);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(248, 249, 250);

        // --- Top Header ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(9, 109, 217),
            Padding = new Padding(20, 12, 20, 12)
        };

        var lblHeaderTitle = new Label
        {
            Text = "📱 THANH TOÁN CHUYỂN KHOẢN QR CODE",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(15, 12)
        };

        var lblHeaderSub = new Label
        {
            Text = "Khách hàng mở ứng dụng Ngân hàng / ZaloPay / Ví điện tử để quét mã bên dưới",
            ForeColor = Color.FromArgb(220, 240, 255),
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Location = new Point(16, 40)
        };

        pnlHeader.Controls.Add(lblHeaderTitle);
        pnlHeader.Controls.Add(lblHeaderSub);

        // --- Left Panel: QR Code Display ---
        var pnlLeft = new Panel
        {
            Location = new Point(20, 85),
            Size = new Size(300, 370),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        picQrCode = new PictureBox
        {
            Size = new Size(260, 260),
            Location = new Point(19, 15),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(250, 250, 250)
        };

        progressBar = new ProgressBar
        {
            Location = new Point(19, 285),
            Size = new Size(260, 8),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30
        };

        var lblQrNote = new Label
        {
            Text = "Hỗ trợ ZaloPay, VietQR & tất cả App Ngân hàng",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(10, 305),
            Size = new Size(280, 45)
        };

        pnlLeft.Controls.Add(picQrCode);
        pnlLeft.Controls.Add(progressBar);
        pnlLeft.Controls.Add(lblQrNote);

        // --- Right Panel: Info & Status ---
        var pnlRight = new Panel
        {
            Location = new Point(335, 85),
            Size = new Size(350, 370),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15)
        };

        var lblOrderTitle = new Label { Text = "MÃ ĐƠN HÀNG:", Location = new Point(15, 15), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9f) };
        var lblOrderVal = new Label { Text = $"#{_orderId}", Location = new Point(130, 14), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

        var lblTransCodeTitle = new Label { Text = "MÃ GIAO DỊCH:", Location = new Point(15, 42), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9f) };
        var lblTransCodeVal = new Label { Text = _transactionCode, Location = new Point(130, 41), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 40) };

        var lblAmountTitle = new Label { Text = "SỐ TIỀN CẦN TRẢ:", Location = new Point(15, 75), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9f) };
        var lblAmountVal = new Label { Text = $"{_amount:N0} ₫", Location = new Point(15, 95), AutoSize = true, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.FromArgb(9, 109, 217) };

        var sep = new Label { BorderStyle = BorderStyle.Fixed3D, Height = 2, Width = 315, Location = new Point(15, 140) };

        var lblStatusTitle = new Label { Text = "TRẠNG THÁI THANH TOÁN:", Location = new Point(15, 150), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

        lblStatusBadge = new Label
        {
            Text = "⏳ ĐANG CHỜ KHÁCH QUÉT MÃ...",
            Location = new Point(15, 175),
            Size = new Size(315, 45),
            BackColor = Color.FromArgb(255, 251, 230),
            ForeColor = Color.FromArgb(212, 136, 6),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblTimer = new Label
        {
            Text = "Mã hết hạn sau: 15:00",
            Location = new Point(15, 230),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.DimGray
        };

        btnManualSync = new Button
        {
            Text = "🔄 Kiểm Tra / Đồng Bộ",
            Location = new Point(15, 260),
            Size = new Size(150, 36),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnManualSync.Click += BtnManualSync_Click;

        btnSimulateSuccess = new Button
        {
            Text = "🧪 Demo Quét Thành Công",
            Location = new Point(172, 260),
            Size = new Size(158, 36),
            BackColor = Color.FromArgb(230, 247, 255),
            ForeColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        btnSimulateSuccess.Click += BtnSimulateSuccess_Click;

        btnCancelPayment = new Button
        {
            Text = "❌ Hủy Giao Dịch",
            Location = new Point(15, 310),
            Size = new Size(315, 40),
            BackColor = Color.FromArgb(255, 241, 240),
            ForeColor = Color.FromArgb(207, 19, 34),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        btnCancelPayment.Click += BtnCancelPayment_Click;

        pnlRight.Controls.Add(lblOrderTitle);
        pnlRight.Controls.Add(lblOrderVal);
        pnlRight.Controls.Add(lblTransCodeTitle);
        pnlRight.Controls.Add(lblTransCodeVal);
        pnlRight.Controls.Add(lblAmountTitle);
        pnlRight.Controls.Add(lblAmountVal);
        pnlRight.Controls.Add(sep);
        pnlRight.Controls.Add(lblStatusTitle);
        pnlRight.Controls.Add(lblStatusBadge);
        pnlRight.Controls.Add(lblTimer);
        pnlRight.Controls.Add(btnManualSync);
        pnlRight.Controls.Add(btnSimulateSuccess);
        pnlRight.Controls.Add(btnCancelPayment);

        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlLeft);
        this.Controls.Add(pnlRight);
    }

    private void LoadQrImage()
    {
        try
        {
            string qrApiUrl;
            if (!string.IsNullOrEmpty(_qrData) && _qrData.StartsWith("http"))
            {
                qrApiUrl = _qrData;
            }
            else
            {
                string encoded = Uri.EscapeDataString(string.IsNullOrEmpty(_qrData) ? _transactionCode : _qrData);
                qrApiUrl = $"https://quickchart.io/qr?text={encoded}&size=260";
            }

            picQrCode.LoadAsync(qrApiUrl);
        }
        catch
        {
            // Fallback sang Google Chart QR
            string encoded = Uri.EscapeDataString(_transactionCode);
            picQrCode.LoadAsync($"https://chart.googleapis.com/chart?chs=260x260&cht=qr&chl={encoded}");
        }
    }

    private void StartTimers()
    {
        // Timer đếm ngược 15 phút
        _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _countdownTimer.Tick += (s, e) =>
        {
            _remainingSeconds--;
            if (_remainingSeconds <= 0)
            {
                _countdownTimer.Stop();
                _pollingTimer.Stop();
                lblTimer.Text = "Mã QR đã HẾT HẠN!";
                lblTimer.ForeColor = Color.Red;
                lblStatusBadge.Text = "❌ GIAO DỊCH ĐÃ HẾT HẠN";
                lblStatusBadge.BackColor = Color.FromArgb(255, 241, 240);
                lblStatusBadge.ForeColor = Color.Red;
                progressBar.Visible = false;
            }
            else
            {
                TimeSpan t = TimeSpan.FromSeconds(_remainingSeconds);
                lblTimer.Text = $"Mã hết hạn sau: {t.Minutes:D2}:{t.Seconds:D2}";
            }
        };
        _countdownTimer.Start();

        // Polling Timer kiểm tra trạng thái tự động mỗi 2.5 giây
        _pollingTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        _pollingTimer.Tick += async (s, e) => await CheckStatusAsync();
        _pollingTimer.Start();
    }

    private async Task CheckStatusAsync()
    {
        try
        {
            var res = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/payments/transactions/{_transactionId}/status");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.GetProperty("isSuccess").GetBoolean())
                {
                    var data = root.GetProperty("data");
                    string status = data.GetProperty("status").GetString() ?? "";

                    if (status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                    {
                        OnPaymentSuccess();
                    }
                    else if (status.Equals("FAILED", StringComparison.OrdinalIgnoreCase) || status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                    {
                        OnPaymentFailed(status);
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi kết nối tạm thời khi polling
        }
    }

    private async void BtnManualSync_Click(object? sender, EventArgs e)
    {
        btnManualSync.Enabled = false;
        btnManualSync.Text = "Đang kiểm tra...";

        try
        {
            var res = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/payments/transactions/{_transactionId}/sync-gateway", null);
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.GetProperty("isSuccess").GetBoolean())
                {
                    var data = root.GetProperty("data");
                    string status = data.GetProperty("status").GetString() ?? "";

                    if (status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                    {
                        OnPaymentSuccess();
                        return;
                    }
                    else
                    {
                        MessageBox.Show("Cổng ZaloPay chưa ghi nhận thanh toán thành công. Vui lòng kiểm tra lại sau khi quét mã.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối kiểm tra: {ex.Message}", "Lỗi");
        }
        finally
        {
            btnManualSync.Enabled = true;
            btnManualSync.Text = "🔄 Kiểm Tra / Đồng Bộ";
        }
    }

    private async void BtnSimulateSuccess_Click(object? sender, EventArgs e)
    {
        // Mô phỏng giả lập ZaloPay gửi Callback thành công để Test/Demo
        btnSimulateSuccess.Enabled = false;
        try
        {
            long appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var callbackDataObj = new
            {
                app_id = 2553,
                app_trans_id = _transactionCode,
                app_time = appTime,
                app_user = "SmartSuperMarket_POS",
                amount = (long)_amount,
                embed_data = "{}",
                item = "[]",
                zp_trans_id = Random.Shared.NextInt64(1000000000, 9999999999),
                server_time = appTime,
                channel = 38
            };

            string dataJson = JsonSerializer.Serialize(callbackDataObj);

            // Tính MAC HMAC-SHA256 giả lập với key2 default
            string key2 = "trH0qqB8LioaKaEajAx6VHrxEFlConstraint";
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(key2));
            string mac = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataJson)));

            var payload = new { data = dataJson, mac = mac, type = 1 };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/payments/zalo-pay/callback", content);
            if (res.IsSuccessStatusCode)
            {
                OnPaymentSuccess();
            }
            else
            {
                MessageBox.Show("Không thể giả lập Callback. Hãy bấm Kiểm Tra / Đồng Bộ.", "Thông báo");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi mô phỏng: {ex.Message}", "Lỗi");
        }
        finally
        {
            btnSimulateSuccess.Enabled = true;
        }
    }

    private void OnPaymentSuccess()
    {
        _pollingTimer?.Stop();
        _countdownTimer?.Stop();

        progressBar.Visible = false;

        lblStatusBadge.Text = "✅ THANH TOÁN THÀNH CÔNG!";
        lblStatusBadge.BackColor = Color.FromArgb(246, 255, 237);
        lblStatusBadge.ForeColor = Color.FromArgb(56, 158, 13);
        lblStatusBadge.BorderStyle = BorderStyle.FixedSingle;

        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch { }

        // Đợi 1.2 giây cho nhân viên nhìn thấy thông báo xanh
        Task.Delay(1200).ContinueWith(_ =>
        {
            if (this.IsHandleCreated)
            {
                this.Invoke(new Action(() =>
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }));
            }
        });
    }

    private void OnPaymentFailed(string status)
    {
        _pollingTimer?.Stop();
        _countdownTimer?.Stop();

        progressBar.Visible = false;

        lblStatusBadge.Text = $"❌ GIAO DỊCH {status.ToUpper()}";
        lblStatusBadge.BackColor = Color.FromArgb(255, 241, 240);
        lblStatusBadge.ForeColor = Color.FromArgb(207, 19, 34);
    }

    private async void BtnCancelPayment_Click(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show("Bạn có chắc muốn HỦY giao dịch thanh toán QR này?", "Xác nhận hủy", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _pollingTimer?.Stop();
        _countdownTimer?.Stop();

        try
        {
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/payments/transactions/{_transactionId}/cancel", null);
        }
        catch { }

        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _pollingTimer?.Stop();
        _countdownTimer?.Stop();
        base.OnFormClosing(e);
    }
}
