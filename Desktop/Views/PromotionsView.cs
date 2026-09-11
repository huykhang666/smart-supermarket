using System;
using System.Drawing;
using System.Windows.Forms;

namespace Desktop.Views;

public class PromotionsView : UserControl
{
    private DataGridView dgvPromotions = null!;

    public PromotionsView()
    {
        InitializeComponent();
        LoadSampleData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlHeader);

        var lblTitle = new Label
        {
            Text = "🎁 CHƯƠNG TRÌNH KHUYẾN MÃI & VOUCHER (PROMOTIONS)",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            Location = new Point(15, 18),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);

        dgvPromotions = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvPromotions);

        dgvPromotions.Columns.Add("Code", "Mã Voucher");
        dgvPromotions.Columns.Add("Name", "Tên Chương Trình");
        dgvPromotions.Columns.Add("Discount", "Mức Giảm Giá");
        dgvPromotions.Columns.Add("Condition", "Điều Kiện Áp Dụng");
        dgvPromotions.Columns.Add("TimeRange", "Thời Gian Hiệu Lực");
        dgvPromotions.Columns.Add("Status", "Trạng Thái");

        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10)
        };
        ThemeManager.ApplyCardPanel(pnlGridContainer);
        pnlGridContainer.Controls.Add(dgvPromotions);

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlHeader);
    }

    private void LoadSampleData()
    {
        dgvPromotions.Rows.Clear();
        dgvPromotions.Rows.Add("KATQ10", "Giảm 10% Đơn Hàng Siêu Thị", "10%", "Đơn từ 200.000 VNĐ", "2026-09-01 -> 2026-09-30", "🟢 Đang Diễn Ra");
        dgvPromotions.Rows.Add("FREESHIP", "Miễn Phí Giao Hàng Bán Lẻ", "30.000 VNĐ", "Đơn từ 500.000 VNĐ", "2026-09-10 -> 2026-10-10", "🟢 Đang Diễn Ra");
        dgvPromotions.Rows.Add("MIDAUTUMN", "Voucher Trung Thu Đặc Biệt", "50.000 VNĐ", "Đơn từ 1.000.000 VNĐ", "2026-09-15 -> 2026-09-25", "⏳ Sắp Đến Hạn");
    }
}
