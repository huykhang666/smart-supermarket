using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class StaffShiftView : UserControl
{
    private Label lblShiftStatus = null!;
    private Label lblShiftTime = null!;
    private NumericUpDown numInitialCash = null!;
    private Label lblTotalOrdersVal = null!;
    private Label lblTotalRevenueVal = null!;
    private Label lblCashRevenueVal = null!;
    private Label lblQrRevenueVal = null!;
    private Label lblCardRevenueVal = null!;
    private NumericUpDown numActualCash = null!;
    private Label lblCashDifference = null!;
    private Button btnOpenShift = null!;
    private Button btnCloseShift = null!;
    private Button btnPrintReport = null!;

    private bool _isShiftOpen = true;

    public StaffShiftView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);
        this.AutoScroll = true;

        // --- 1. Page Header ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "👤 Quản Lý Ca Làm Việc & Kết Ca Bán Hàng",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Kiểm soát tiền mặt đầu ca, tổng doanh thu phương thức thanh toán và bàn giao kết ca thu ngân",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. Main Grid Layout (2 Columns: Left = Shift Status & Controls, Right = Financial Summary) ---
        var pnlMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // LEFT CARD: Thông tin ca & Mở/Đóng ca
        var pnlLeftCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(20),
            Margin = new Padding(0, 8, 10, 0)
        };
        AppTheme.ApplyCardPanel(pnlLeftCard);

        var lblLeftCardTitle = new Label
        {
            Text = "🕒 THÔNG TIN CA TRỰC HIỆN TẠI",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 16),
            AutoSize = true
        };

        var pnlShiftBadge = new Panel
        {
            Location = new Point(16, 50),
            Size = new Size(200, 32),
            BackColor = AppTheme.SuccessSubtle
        };
        lblShiftStatus = new Label
        {
            Text = "🟢 Trạng thái: ĐANG MỞ CA",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.Success,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlShiftBadge.Controls.Add(lblShiftStatus);

        lblShiftTime = new Label
        {
            Text = "Ca làm: 08:00 → 17:00  (Ca sáng - Thu ngân: Thu Thảo)",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(16, 92),
            AutoSize = true
        };

        var lblDivider1 = new Panel { Location = new Point(16, 122), Size = new Size(pnlLeftCard.Width - 32, 1), BackColor = AppTheme.BorderLight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        var lblInitCashTitle = new Label
        {
            Text = "💵 Tiền Mặt Đầu Ca (VNĐ):",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 135),
            AutoSize = true
        };

        numInitialCash = new NumericUpDown
        {
            Location = new Point(16, 162),
            Size = new Size(280, 32),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Maximum = 100000000,
            Minimum = 0,
            Value = 0,
            ThousandsSeparator = true,
            DecimalPlaces = 0
        };
        numInitialCash.ValueChanged += (s, e) => RecalculateDifference();

        var lblActualCashTitle = new Label
        {
            Text = "💰 Tiền Mặt Kiểm Thực Tế Cuối Ca (VNĐ):",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 212),
            AutoSize = true
        };

        numActualCash = new NumericUpDown
        {
            Location = new Point(16, 238),
            Size = new Size(280, 32),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Maximum = 500000000,
            Minimum = 0,
            Value = 0,
            ThousandsSeparator = true,
            DecimalPlaces = 0
        };
        numActualCash.ValueChanged += (s, e) => RecalculateDifference();

        var lblDiffTitle = new Label
        {
            Text = "Chênh lệch tiền mặt thực tế vs sổ sách:",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(16, 285),
            AutoSize = true
        };

        lblCashDifference = new Label
        {
            Text = "0 VNĐ (Khớp tiền bàn giao)",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = AppTheme.Success,
            Location = new Point(16, 310),
            AutoSize = true
        };

        var pnlShiftButtons = new FlowLayoutPanel
        {
            Location = new Point(16, 360),
            Size = new Size(Math.Max(300, pnlLeftCard.Width - 32), 95),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent
        };

        btnOpenShift = new Button
        {
            Text = "🔓 Mở Ca Trực Mới",
            Size = new Size(170, 36),
            Margin = new Padding(0, 0, 8, 8)
        };
        AppTheme.ApplySecondaryButton(btnOpenShift);
        btnOpenShift.Click += (s, e) =>
        {
            _isShiftOpen = true;
            lblShiftStatus.Text = "🟢 Trạng thái: ĐANG MỞ CA";
            pnlShiftBadge.BackColor = AppTheme.SuccessSubtle;
            lblShiftStatus.ForeColor = AppTheme.Success;
            AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã ghi nhận mở ca trực mới thành công.");
        };

        btnCloseShift = new Button
        {
            Text = "🔒 Chốt & Đóng Ca Trực",
            Size = new Size(180, 36),
            Margin = new Padding(0, 0, 8, 8)
        };
        AppTheme.ApplyDangerButton(btnCloseShift);
        btnCloseShift.Click += (s, e) =>
        {
            var res = MessageBox.Show(
                "Bạn có chắc chắn muốn CHỐT và ĐÓNG CA bán hàng hiện tại?\n\nHệ thống sẽ in phiếu bàn giao tiền mặt và khóa phiên làm việc.",
                "Xác nhận đóng ca", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                _isShiftOpen = false;
                lblShiftStatus.Text = "🔴 Trạng thái: ĐÃ ĐÓNG CA";
                pnlShiftBadge.BackColor = AppTheme.DangerSubtle;
                lblShiftStatus.ForeColor = AppTheme.Danger;
                AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã đóng ca thành công. Vui lòng in biên bản bàn giao!");
            }
        };

        btnPrintReport = new Button
        {
            Text = "🖨️ In Báo Cáo Bàn Giao Ca (F10)",
            Size = new Size(358, 36),
            Margin = new Padding(0)
        };
        AppTheme.ApplyPrimaryButton(btnPrintReport);
        btnPrintReport.Click += (s, e) =>
        {
            MessageBox.Show(
                $"================ BÁO CÁO KẾT CA BÁN HÀNG ================\n" +
                $"Siêu Thị: Smart SuperMarket - Phân Hệ POS Thu Ngân\n" +
                $"Thu ngân: Thu Thảo  |  Mã ca: SHIFT-2026-01\n" +
                $"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                $"---------------------------------------------------------\n" +
                $"• Số lượng đơn bán: 152 hóa đơn\n" +
                $"• Tiền mặt đầu ca: {numInitialCash.Value:N0} VNĐ\n" +
                $"• Doanh thu Tiền mặt: 12,000,000 VNĐ\n" +
                $"• Doanh thu VietQR: 10,000,000 VNĐ\n" +
                $"• Doanh thu Thẻ POS: 3,000,000 VNĐ\n" +
                $"---------------------------------------------------------\n" +
                $"TỔNG DOANH THU CA: 25,000,000 VNĐ\n" +
                $"Tiền mặt thực tế: {numActualCash.Value:N0} VNĐ\n" +
                $"Chênh lệch: {lblCashDifference.Text}\n" +
                $"=========================================================\n" +
                $"Đã gửi lệnh in tới máy in nhiệt hóa đơn POS-80!",
                "In báo cáo bàn giao", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        pnlShiftButtons.Controls.Add(btnOpenShift);
        pnlShiftButtons.Controls.Add(btnCloseShift);
        pnlShiftButtons.Controls.Add(btnPrintReport);

        pnlLeftCard.Controls.Add(lblLeftCardTitle);
        pnlLeftCard.Controls.Add(pnlShiftBadge);
        pnlLeftCard.Controls.Add(lblShiftTime);
        pnlLeftCard.Controls.Add(lblDivider1);
        pnlLeftCard.Controls.Add(lblInitCashTitle);
        pnlLeftCard.Controls.Add(numInitialCash);
        pnlLeftCard.Controls.Add(lblActualCashTitle);
        pnlLeftCard.Controls.Add(numActualCash);
        pnlLeftCard.Controls.Add(lblDiffTitle);
        pnlLeftCard.Controls.Add(lblCashDifference);
        pnlLeftCard.Controls.Add(pnlShiftButtons);

        // RIGHT CARD: Báo cáo tài chính ca
        var pnlRightCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(20),
            Margin = new Padding(10, 8, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlRightCard);

        var lblRightCardTitle = new Label
        {
            Text = "📊 DOANH THU & PHÂN BỔ THANH TOÁN CA",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(16, 16),
            AutoSize = true
        };

        // Orders stat
        var lblOrdersTitle = new Label { Text = "Tổng hóa đơn đã lập:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Location = new Point(16, 60), AutoSize = true };
        lblTotalOrdersVal = new Label { Text = "0 hóa đơn", Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(220, 60), AutoSize = true };

        // Revenue total
        var lblRevTitle = new Label { Text = "Tổng doanh thu ca:", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, Location = new Point(16, 95), AutoSize = true };
        lblTotalRevenueVal = new Label { Text = "0 VNĐ", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = AppTheme.Primary, Location = new Point(218, 92), AutoSize = true };

        var lblDivider2 = new Panel { Location = new Point(16, 130), Size = new Size(pnlRightCard.Width - 32, 1), BackColor = AppTheme.BorderLight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        // Method 1: Tiền mặt
        var pnlPayCash = CreatePaymentStatRow("💵 Tiền Mặt (Cash)", "0 VNĐ", "Chiếm 0.0%", AppTheme.Success, 145);
        // Method 2: VietQR
        var pnlPayQr = CreatePaymentStatRow("📱 Quét mã VietQR", "0 VNĐ", "Chiếm 0.0%", AppTheme.Primary, 215);
        // Method 3: Thẻ POS
        var pnlPayCard = CreatePaymentStatRow("💳 Quẹt thẻ ngân hàng POS", "0 VNĐ", "Chiếm 0.0%", AppTheme.Warning, 285);

        pnlRightCard.Controls.Add(lblRightCardTitle);
        pnlRightCard.Controls.Add(lblOrdersTitle);
        pnlRightCard.Controls.Add(lblTotalOrdersVal);
        pnlRightCard.Controls.Add(lblRevTitle);
        pnlRightCard.Controls.Add(lblTotalRevenueVal);
        pnlRightCard.Controls.Add(lblDivider2);
        pnlRightCard.Controls.Add(pnlPayCash);
        pnlRightCard.Controls.Add(pnlPayQr);
        pnlRightCard.Controls.Add(pnlPayCard);

        pnlMain.Controls.Add(pnlLeftCard, 0, 0);
        pnlMain.Controls.Add(pnlRightCard, 1, 0);

        this.Controls.Add(pnlMain);
        this.Controls.Add(pnlHeader);
    }

    private Panel CreatePaymentStatRow(string name, string amount, string percent, Color accent, int y)
    {
        var p = new Panel
        {
            Location = new Point(16, y),
            Size = new Size(420, 58),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = AppTheme.BackgroundGray
        };
        p.Paint += (s, e) =>
        {
            using var b = new SolidBrush(accent);
            e.Graphics.FillRectangle(b, 0, 0, 4, p.Height);
        };

        var lblN = new Label { Text = name, Font = AppTheme.FontBodyBold, ForeColor = AppTheme.TextPrimary, Location = new Point(14, 8), AutoSize = true };
        var lblA = new Label { Text = amount, Font = AppTheme.FontBodyBold, ForeColor = accent, Location = new Point(14, 30), AutoSize = true };
        var lblP = new Label { Text = percent, Font = AppTheme.FontCaption, ForeColor = AppTheme.TextSecondary, Location = new Point(320, 18), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };

        p.Controls.Add(lblN);
        p.Controls.Add(lblA);
        p.Controls.Add(lblP);
        return p;
    }

    private void RecalculateDifference()
    {
        decimal expectedCash = numInitialCash.Value;
        decimal actualCash = numActualCash.Value;
        decimal diff = actualCash - expectedCash;

        if (diff == 0)
        {
            lblCashDifference.Text = "0 VNĐ (Khớp tiền bàn giao)";
            lblCashDifference.ForeColor = AppTheme.Success;
        }
        else if (diff > 0)
        {
            lblCashDifference.Text = $"+{diff:N0} VNĐ (Thừa tiền bàn giao)";
            lblCashDifference.ForeColor = AppTheme.Success;
        }
        else
        {
            lblCashDifference.Text = $"-{Math.Abs(diff):N0} VNĐ (⚠️ Thiếu hụt tiền mặt)";
            lblCashDifference.ForeColor = AppTheme.Danger;
        }
    }
}
