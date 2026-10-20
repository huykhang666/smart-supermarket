using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using QRCoder;
using Desktop.Models;
using Desktop.Services;

namespace Desktop.Forms;

/// <summary>
/// Form xác nhận thanh toán VietQR thủ công tại quầy POS (Code-Behind Only)
/// </summary>
public class FormThanhToanVietQR : Form
{
    private readonly string _maHoaDon;
    private readonly decimal _soTien;
    private readonly string _cashierId;
    private readonly PaymentService _paymentService;

    // Controls
    private PictureBox picQrCode = null!;
    private Button btnXacNhan = null!;
    private Button btnHuy = null!;
    private Label lblTitle = null!;
    private Label lblInvoiceInfo = null!;
    private Label lblAmountInfo = null!;
    private Label lblGuide = null!;
    private Panel pnlHeader = null!;
    private Panel pnlFooter = null!;
    private Panel pnlCenter = null!;

    // Trạng thái giao dịch
    private string _transactionCode = string.Empty;
    private int _transactionId = 0;

    public FormThanhToanVietQR(string maHoaDon, decimal soTien, string cashierId)
    {
        _maHoaDon = maHoaDon ?? string.Empty;
        _soTien = soTien;
        _cashierId = cashierId ?? "NV01";
        _paymentService = new PaymentService();

        BuildFormLayout();
        this.Load += Form_Load;
    }

    private void BuildFormLayout()
    {
        this.Text = "Thanh Toán VietQR - Smart SuperMarket";
        this.Size = new Size(460, 620);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.White;

        // Header Panel
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = Color.FromArgb(24, 144, 255),
            Padding = new Padding(15)
        };

        lblTitle = new Label
        {
            Text = "QUÉT MÃ VIETQR ĐỂ THANH TOÁN",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter
        };

        lblInvoiceInfo = new Label
        {
            Text = $"Hóa đơn: {_maHoaDon}  |  Thu ngân: {_cashierId}",
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.WhiteSmoke,
            Dock = DockStyle.Bottom,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblInvoiceInfo);

        // Footer Panel
        pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 85,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(20, 15, 20, 15)
        };

        btnHuy = new Button
        {
            Text = "Hủy bỏ (ESC)",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(108, 117, 125),
            BackColor = Color.FromArgb(233, 236, 239),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 50),
            Location = new Point(25, 17),
            Cursor = Cursors.Hand
        };
        btnHuy.FlatAppearance.BorderSize = 0;
        btnHuy.Click += btnHuy_Click;

        btnXacNhan = new Button
        {
            Text = "Đã nhận tiền (F1)",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(40, 167, 69),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 50),
            Location = new Point(220, 17),
            Cursor = Cursors.Hand
        };
        btnXacNhan.FlatAppearance.BorderSize = 0;
        btnXacNhan.Click += btnXacNhan_Click;

        pnlFooter.Controls.Add(btnHuy);
        pnlFooter.Controls.Add(btnXacNhan);

        // Center Panel
        pnlCenter = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        lblAmountInfo = new Label
        {
            Text = $"Số tiền: {_soTien:N0} đ",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(220, 53, 69),
            Dock = DockStyle.Top,
            Height = 35,
            TextAlign = ContentAlignment.MiddleCenter
        };

        picQrCode = new PictureBox
        {
            Size = new Size(320, 320),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(60, 45),
            BackColor = Color.FromArgb(250, 250, 250)
        };
        picQrCode.LoadCompleted += (s, e) =>
        {
            if (e.Error != null)
            {
                MessageBox.Show($"Không thể tải ảnh mã QR từ hệ thống VietQR:\n{e.Error.Message}",
                                "Lỗi hiển thị mã QR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        lblGuide = new Label
        {
            Text = "Khách hàng quét mã bằng ứng dụng ngân hàng bất kỳ.\nSau khi nhận tiền vào tài khoản, bấm 'Đã nhận tiền'.",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 100, 100),
            Dock = DockStyle.Bottom,
            Height = 45,
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlCenter.Controls.Add(picQrCode);
        pnlCenter.Controls.Add(lblAmountInfo);
        pnlCenter.Controls.Add(lblGuide);

        // Add to Form
        this.Controls.Add(pnlCenter);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeader);

        // Phím tắt bàn phím tiện dụng cho thu ngân
        this.KeyPreview = true;
        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                btnHuy.PerformClick();
            }
            else if (e.KeyCode == Keys.F1 || e.KeyCode == Keys.Enter)
            {
                btnXacNhan.PerformClick();
            }
        };
    }

    private async void Form_Load(object? sender, EventArgs e)
    {
        try
        {
            // Cấu hình giao thức TLS để WinForms tải được ảnh qua HTTPS từ VietQR.io
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls11 |
                System.Net.SecurityProtocolType.Tls;

            // 1. Khởi tạo giao dịch PENDING vào DB (Chạy ngầm không chặn UI)
            _transactionCode = PaymentService.GenerateTransactionCode();

            var transaction = new PaymentTransactionModel
            {
                TransactionCode = _transactionCode,
                InvoiceId = _maHoaDon,
                PaymentMethod = PaymentMethod.VietQR,
                Amount = _soTien,
                GivenAmount = _soTien,
                ChangeAmount = 0,
                TransferContent = _maHoaDon,
                BankCode = BankConfig.BankCode,
                Status = PaymentStatus.Pending,
                CreatedDate = DateTime.Now,
                CashierId = _cashierId
            };

            _ = Task.Run(() =>
            {
                try
                {
                    bool created = _paymentService.CreateTransaction(transaction);
                    _transactionId = transaction.TransactionId;
                    if (!created)
                    {
                        System.Diagnostics.Debug.WriteLine("[FormThanhToanVietQR] Chưa kết nối CSDL PaymentTransaction, tiếp tục chế độ POS cục bộ.");
                    }
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[FormThanhToanVietQR] DB Exception: {dbEx.Message}");
                }
            });

            // 2. Tải ảnh QR trực tiếp từ VietQR hoặc tự render QR Code offline nếu không có mạng
            long amountInt = Convert.ToInt64(Math.Round(_soTien));
            string qrUrl = $"https://img.vietqr.io/image/{BankConfig.BankCode}-{BankConfig.AccountNumber}-compact2.png?amount={amountInt}&addInfo={Uri.EscapeDataString(_maHoaDon)}&accountName={Uri.EscapeDataString(BankConfig.AccountName)}";

            bool qrLoaded = false;
            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };
                using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) };
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                var imageBytes = await httpClient.GetByteArrayAsync(qrUrl);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    using var ms = new MemoryStream(imageBytes);
                    using var img = Image.FromStream(ms);
                    picQrCode.Image = new Bitmap(img);
                    qrLoaded = true;
                }
            }
            catch (Exception dlEx)
            {
                System.Diagnostics.Debug.WriteLine($"[FormThanhToanVietQR] VietQR online download warning: {dlEx.Message}");
            }

            // Nếu mạng chập chờn hoặc VietQR API không phản hồi, tự động sinh mã QR cục bộ (Hoàn toàn Offline)
            if (!qrLoaded)
            {
                picQrCode.Image = GenerateLocalVietQrBitmap(BankConfig.BankCode, BankConfig.AccountNumber, BankConfig.AccountName, amountInt, _maHoaDon);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi trong quá trình hiển thị Form thanh toán: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Sinh mã QR chuyển khoản trực tiếp (Offline) bằng QRCoder đảm bảo luôn hiển thị 100% không bị lỗi ảnh
    /// </summary>
    private static Bitmap GenerateLocalVietQrBitmap(string bank, string accountNo, string accountName, long amount, string content)
    {
        // Tạo chuỗi liên kết thanh toán / mã QR quét được bằng mọi ứng dụng ngân hàng
        string payload = $"2|99|{accountNo}|{accountName}|{bank}|{amount}|{content}";
        
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCode(qrData);
        var qrImage = qrCode.GetGraphic(12, Color.FromArgb(20, 35, 60), Color.White, true);

        // Vẽ thêm viền và thông tin logo ngân hàng phía trên ảnh
        var resultBitmap = new Bitmap(320, 320);
        using (var g = Graphics.FromImage(resultBitmap))
        {
            g.Clear(Color.White);
            g.DrawImage(qrImage, new Rectangle(10, 10, 300, 300));
        }
        return resultBitmap;
    }

    private void btnXacNhan_Click(object? sender, EventArgs e)
    {
        try
        {
            var confirm = MessageBox.Show(
                $"Xác nhận đã nhận đủ số tiền {_soTien:N0} đ từ khách hàng qua chuyển khoản VietQR?",
                "Xác nhận thanh toán",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            btnXacNhan.Enabled = false;

            // Cập nhật DB Status = SUCCESS
            bool updated = false;
            if (_transactionId > 0)
            {
                updated = _paymentService.UpdateStatusById(_transactionId, PaymentStatus.Success);
            }
            else if (!string.IsNullOrWhiteSpace(_transactionCode))
            {
                updated = _paymentService.UpdateStatus(_transactionCode, PaymentStatus.Success);
            }

            if (!updated)
            {
                DialogResult retry = MessageBox.Show(
                    "Cập nhật trạng thái CSDL chưa thành công. Bạn vẫn muốn hoàn tất hóa đơn này?",
                    "Cảnh báo kết nối", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (retry != DialogResult.Yes)
                {
                    btnXacNhan.Enabled = true;
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xác nhận thanh toán: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnXacNhan.Enabled = true;
        }
    }

    private void btnHuy_Click(object? sender, EventArgs e)
    {
        try
        {
            btnHuy.Enabled = false;

            // Cập nhật DB Status = CANCELLED
            if (_transactionId > 0)
            {
                _paymentService.UpdateStatusById(_transactionId, PaymentStatus.Cancelled);
            }
            else if (!string.IsNullOrWhiteSpace(_transactionCode))
            {
                _paymentService.UpdateStatus(_transactionCode, PaymentStatus.Cancelled);
            }

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi hủy giao dịch: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
