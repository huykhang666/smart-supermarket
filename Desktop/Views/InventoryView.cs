using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class InventoryView : UserControl
{
    private TabControl tabInventory = null!;
    private TabPage tabStockList = null!;
    private TabPage tabAudit = null!;
    private TabPage tabExpiry = null!;
    private TabPage tabTransfer = null!;

    public InventoryView()
    {
        InitializeComponent();
        LoadStockData();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        // --- 1. Page Header ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "📦 Quản Lý Kho Hàng & Hạn Sử Dụng (Warehouse ERP)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Kiểm soát lượng tồn thực tế, theo dõi date hàng nhập, phân vùng kệ hàng và điều phối kho nội bộ",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. Dashboard Nhỏ: 4 KPI Cards (Tổng sản phẩm 0, Hết hàng 0, Sắp hết 0, HSD ≤ 5 ngày 0) ---
        var pnlKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 8)
        };
        pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        pnlKpis.Controls.Add(AppTheme.CreateKpiCard("TỔNG SẢN PHẨM KHO", "0", "📦 Toàn bộ SKU đang lưu", AppTheme.Primary), 0, 0);
        pnlKpis.Controls.Add(AppTheme.CreateKpiCard("HẾT HÀNG TỒN QUẦY", "0", "🔴 Cần châm hàng ngay", AppTheme.Danger), 1, 0);
        pnlKpis.Controls.Add(AppTheme.CreateKpiCard("SẮP HẾT HÀNG", "0", "⚠️ Dưới mức tồn an toàn", AppTheme.Warning), 2, 0);
        pnlKpis.Controls.Add(AppTheme.CreateKpiCard("CẬN HSD (≤ 5 NGÀY)", "0", "⏳ Cần xả hàng / dán tem", AppTheme.Warning), 3, 0);

        // --- 3. TabControl Kho (Danh sách tồn, Kiểm kê, HSD, Chuyển kho) ---
        tabInventory = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBodyBold
        };

        tabStockList = new TabPage("📋 Danh Sách Tồn Kho");
        tabAudit = new TabPage("🔍 Kiểm Kê Nhanh");
        tabExpiry = new TabPage("⏳ Cảnh Báo Hạn Sử Dụng (HSD)");
        tabTransfer = new TabPage("🔄 Chuyển Kho Nội Bộ");

        BuildStockListTab();
        BuildExpiryTab();
        BuildTransferTab();

        // Integrate StockAuditView into tabAudit
        var auditControl = new StockAuditView { Dock = DockStyle.Fill, Padding = new Padding(12) };
        tabAudit.Controls.Add(auditControl);

        tabInventory.TabPages.Add(tabStockList);
        tabInventory.TabPages.Add(tabAudit);
        tabInventory.TabPages.Add(tabExpiry);
        tabInventory.TabPages.Add(tabTransfer);

        this.Controls.Add(tabInventory);
        this.Controls.Add(pnlKpis);
        this.Controls.Add(pnlHeader);
    }

    private void BuildStockListTab()
    {
        var dgv = new DataGridView();
        AppTheme.ApplyGridStyle(dgv);
        dgv.Dock = DockStyle.Fill;
        dgv.Columns.Add("Barcode", "Mã Barcode");
        dgv.Columns.Add("Name", "Tên Sản Phẩm");
        dgv.Columns.Add("Stock", "Số Lượng Tồn");
        dgv.Columns.Add("Location", "Vị Trí Kệ");
        dgv.Columns.Add("Status", "Trạng Thái");

        tabStockList.Controls.Add(dgv);
    }

    private void BuildExpiryTab()
    {
        var dgv = new DataGridView();
        AppTheme.ApplyGridStyle(dgv);
        dgv.Dock = DockStyle.Fill;
        dgv.Columns.Add("Batch", "Mã Lô Hàng");
        dgv.Columns.Add("Name", "Tên Sản Phẩm");
        dgv.Columns.Add("Qty", "SL Còn Lại");
        dgv.Columns.Add("ExpiryDate", "Hạn Sử Dụng");
        dgv.Columns.Add("DaysLeft", "Số Ngày Còn Lại");
        dgv.Columns.Add("Action", "Khuyến Nghị AI");

        tabExpiry.Controls.Add(dgv);
    }

    private void BuildTransferTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.SurfaceWhite, Padding = new Padding(24) };
        var lblInfo = new Label
        {
            Text = "🔄 Chức Năng Điều Chuyển Hàng Nội Bộ (Kho Tổng → Kho Bán Lẻ Quầy):\n\n" +
                   "• Hỗ trợ tạo phiếu điều chuyển hàng hóa từ Kho lưu trữ tầng hầm lên quầy kệ trưng bày POS.\n" +
                   "• Cập nhật số liệu tức thì giữa các phân khu trong siêu thị.\n\n" +
                   "Hiện tại kho quầy đang ở trạng thái ổn định.",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Fill
        };
        pnl.Controls.Add(lblInfo);
        tabTransfer.Controls.Add(pnl);
    }

    private void LoadStockData()
    {
    }
}
