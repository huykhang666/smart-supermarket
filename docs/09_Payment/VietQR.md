# Tích Hợp Thanh Toán VietQR Trong C# WinForms (Smart SuperMarket)

## 1. Mã Nguồn C# WinForms (`FormThanhToanVietQR.cs`)

```csharp
using System;
using System.Windows.Forms;

namespace SmartSuperMarket.POS
{
    public partial class FormThanhToanVietQR : Form
    {
        // Cấu hình tài khoản ngân hàng siêu thị Smart SuperMarket
        private readonly string _bankCode = "MB";                 // Mã ngân hàng (MB, VCB, ICB, TCB...)
        private readonly string _accountNumber = "0331000123456";  // Số tài khoản ngân hàng
        private readonly string _accountName = "SMART SUPERMARKET"; // Tên chủ tài khoản (In hoa không dấu)

        private string _maHoaDon;
        private decimal _soTien;

        public FormThanhToanVietQR(string maHoaDon, decimal soTien)
        {
            InitializeComponent();
            _maHoaDon = maHoaDon;
            _soTien = soTien;
        }

        private void FormThanhToanVietQR_Load(object sender, EventArgs e)
        {
            HienThiMaQR();
        }

        // Sinh URL VietQR và hiển thị lên PictureBox bất đồng bộ
        private void HienThiMaQR()
        {
            string noiDungEncoded = Uri.EscapeDataString(_maHoaDon);
            string tenTaiKhoanEncoded = Uri.EscapeDataString(_accountName);

            // Tạo đường dẫn ảnh QR VietQR Quick Link
            string qrUrl = $"[https://img.vietqr.io/image/](https://img.vietqr.io/image/){_bankCode}-{_accountNumber}-compact2.png?amount={_soTien}&addInfo={noiDungEncoded}&accountName={tenTaiKhoanEncoded}";

            // Tải ảnh trực tiếp vào PictureBox không gây đơ giao diện
            picQrCode.LoadAsync(qrUrl);
            
            lblSoTien.Text = string.Format("{0:N0} VNĐ", _soTien);
            lblNoiDung.Text = _maHoaDon;
        }

        // Thu ngân nghe loa báo / kiểm tra app ngân hàng rồi bấm xác nhận
        private void btnXacNhanDaNhanTien_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                $"Xác nhận đã nhận đủ {string.Format("{0:N0} VNĐ", _soTien)} cho đơn hàng {_maHoaDon}?",
                "Xác nhận thanh toán",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dr == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        // Nút hủy nếu khách đổi ý không muốn chuyển khoản nữa
        private void btnHuyGiaoDich_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}