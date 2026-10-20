using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
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

        // Phím tắt bàn phím
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
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls11 |
                System.Net.SecurityProtocolType.Tls;

            // 1. Tạo giao dịch PENDING trong CSDL
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
                    _paymentService.CreateTransaction(transaction);
                    _transactionId = transaction.TransactionId;
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[FormThanhToanVietQR] DB Exception: {dbEx.Message}");
                }
            });

            // 2. Tải ảnh VietQR Online (Nếu có kết nối Internet)
            long amountInt = Convert.ToInt64(Math.Round(_soTien));
            string bankCode = BankConfig.BankCode.Trim().ToUpper();
            string accountNumber = BankConfig.AccountNumber.Trim();
            string accountName = BankConfig.AccountName.Trim();

            string qrUrl = $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.png" +
                           $"?amount={amountInt}" +
                           $"&addInfo={Uri.EscapeDataString(_maHoaDon)}" +
                           $"&accountName={Uri.EscapeDataString(accountName)}";

            bool qrLoaded = false;
            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };
                using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

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

            // 3. Nếu không tải được online -> Tự sinh mã VietQR chuẩn EMVCo Napas trực tiếp (Đảm bảo quét được 100%)
            if (!qrLoaded)
            {
                picQrCode.Image = GenerateStandardVietQrEmvBitmap(bankCode, accountNumber, amountInt, _maHoaDon);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi hiển thị Form thanh toán: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Tạo mã QR theo đúng tiêu chuẩn EMVCo VietQR của Napas (Quét được trên 100% App Ngân Hàng Việt Nam)
    /// </summary>
    private static Bitmap GenerateStandardVietQrEmvBitmap(string bankCode, string accountNumber, long amount, string content)
    {
        string emvPayload = BuildVietQrEmvCoPayload(bankCode, accountNumber, amount, content);

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(emvPayload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCode(qrData);
        var qrImage = qrCode.GetGraphic(12, Color.FromArgb(20, 35, 60), Color.White, true);

        var resultBitmap = new Bitmap(320, 320);
        using (var g = Graphics.FromImage(resultBitmap))
        {
            g.Clear(Color.White);
            g.DrawImage(qrImage, new Rectangle(10, 10, 300, 300));
        }
        return resultBitmap;
    }

    /// <summary>
    /// Xây dựng chuỗi dữ liệu EMVCo VietQR chuẩn Napas
    /// </summary>
    private static string BuildVietQrEmvCoPayload(string bankCode, string accountNumber, long amount, string content)
    {
        string bin = GetBankBin(bankCode);
        string acc = accountNumber.Trim();
        string addInfo = RemoveVietnameseAccents(content.Trim());

        // Sub-tag 00 (BIN 6 số) + Sub-tag 01 (Số tài khoản)
        string sub00 = $"0006{bin}";
        string sub01 = $"01{acc.Length:D2}{acc}";
        string beneficiaryStr = sub00 + sub01;

        // Tag 38: Thông tin tài khoản thụ hưởng
        string tag38_sub00 = "0010A000000727"; // GUID Napas
        string tag38_sub01 = $"01{beneficiaryStr.Length:D2}{beneficiaryStr}";
        string tag38_sub02 = "0208QRIBFTTA"; // Mã dịch vụ Chuyển tiền tới tài khoản
        string tag38_val = tag38_sub00 + tag38_sub01 + tag38_sub02;
        string tag38 = $"38{tag38_val.Length:D2}{tag38_val}";

        // Tag 53: Đơn vị tiền tệ (704 = VND)
        string tag53 = "5303704";

        // Tag 54: Số tiền
        string amountStr = amount.ToString();
        string tag54 = amount > 0 ? $"54{amountStr.Length:D2}{amountStr}" : "";

        // Tag 58: Mã quốc gia (VN)
        string tag58 = "5802VN";

        // Tag 62: Nội dung chuyển khoản
        string tag62 = "";
        if (!string.IsNullOrEmpty(addInfo))
        {
            string sub08 = $"08{addInfo.Length:D2}{addInfo}";
            tag62 = $"62{sub08.Length:D2}{sub08}";
        }

        // Chuỗi chưa có CRC (Tag 00 = 000201, Tag 01 = 010212 dynamic)
        string raw = $"000201010212{tag38}{tag53}{tag54}{tag58}{tag62}6304";

        // Tính toán CRC16-CCITT Checksum
        ushort crc = ComputeCrc16Ccitt(Encoding.ASCII.GetBytes(raw));
        return raw + crc.ToString("X4");
    }

    /// <summary>
    /// Tra cứu mã BIN 6 số chuẩn Ngân hàng Nhà nước cho VietQR
    /// </summary>
    private static string GetBankBin(string bankCode)
    {
        if (string.IsNullOrWhiteSpace(bankCode)) return "970422";
        string code = bankCode.Trim().ToUpper();

        if (code.Length == 6 && long.TryParse(code, out _)) return code;

        return code switch
        {
            "MB" or "MBBANK" => "970422",
            "VCB" or "VIETCOMBANK" => "970436",
            "ICB" or "VIETINBANK" => "970415",
            "BIDV" => "970418",
            "TCB" or "TECHCOMBANK" => "970407",
            "VBA" or "AGRIBANK" => "970405",
            "ACB" => "970416",
            "TPB" or "TPBANK" => "970423",
            "VPB" or "VPBANK" => "970432",
            "STB" or "SACOMBANK" => "970403",
            "HDB" or "HDBANK" => "970437",
            "MSB" => "970426",
            "SHB" => "970443",
            "LPB" or "LPBANK" => "970449",
            "EIB" or "EXIMBANK" => "970431",
            "VIB" => "970441",
            "OCB" => "970448",
            "SEAB" or "SEABANK" => "970440",
            _ => "970422"
        };
    }

    /// <summary>
    /// Thuật toán tính CRC16-CCITT
    /// </summary>
    private static ushort ComputeCrc16Ccitt(byte[] bytes)
    {
        ushort crc = 0xFFFF;
        for (int i = 0; i < bytes.Length; i++)
        {
            crc ^= (ushort)(bytes[i] << 8);
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc = (ushort)(crc << 1);
            }
        }
        return crc;
    }

    /// <summary>
    /// Bỏ dấu tiếng Việt cho nội dung chuyển tiền
    /// </summary>
    private static string RemoveVietnameseAccents(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string[] signs = new string[]
        {
            "aàảãáạăằẳẵắặâầẩẫấậ", "AÀẢÃÁẠĂẰẲẴẮẶÂẦẨẪẤẬ",
            "dđ", "DĐ",
            "eèẻẽéẹêềểễếệ", "EÈẺỄẾỆ",
            "iìỉĩíị", "IÌỈĨÍỊ",
            "oòỏõóọôồổỗốộơờởỡớợ", "OÒỎÕÓỌÔỒỔỖỐỘƠỜỞỠỚỢ",
            "uùủũúụưừửữứự", "UÙỦŨÚỤƯỪỬỮỨỰ",
            "yỳỷỹýỵ", "YỲỶỸÝỴ"
        };
        for (int i = 1; i < signs.Length; i++)
        {
            for (int j = 0; j < signs[i].Length; j++)
                text = text.Replace(signs[i][j], signs[0][i - 1]);
        }
        return text;
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

            if (confirm != DialogResult.Yes) return;

            btnXacNhan.Enabled = false;

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