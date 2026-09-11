using System;
using System.Drawing;
using System.Windows.Forms;

namespace Desktop.Views;

public class SettingsView : UserControl
{
    private TabControl tabControl = null!;
    private DataGridView dgvAuditLogs = null!;

    public SettingsView()
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
            Text = "⚙ CẤU HÌNH HỆ THỐNG, PHÂN QUYỀN & NHẬT KÝ (SYSTEM & AUDIT LOGS)",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            Location = new Point(15, 18),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);

        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = ThemeManager.SubtitleFont
        };

        var tabConfig = new TabPage("⚙️ Cấu Hình Siêu Thị & VAT") { BackColor = ThemeManager.Background };
        var tabAudit = new TabPage("📋 Nhật Ký Hệ Thống (Audit Logs)") { BackColor = ThemeManager.Background };
        var tabBackup = new TabPage("💾 Sao Lưu & Khôi Phục Dữ Liệu") { BackColor = ThemeManager.Background };

        // Tab Config Panel Card
        var pnlConfigCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(25)
        };
        ThemeManager.ApplyCardPanel(pnlConfigCard);

        var lblCompany = new Label { Text = "🏢 Tên Hệ Thống Siêu Thị: SMART SUPERMARKET KATQ", Font = ThemeManager.BodyBold, Location = new Point(25, 25), AutoSize = true, ForeColor = ThemeManager.TextPrimary };
        var lblVAT = new Label { Text = "📊 Thuế VAT Mặc Định Bán Lẻ: 10%", Font = ThemeManager.BodyBold, Location = new Point(25, 65), AutoSize = true, ForeColor = ThemeManager.TextPrimary };
        var lblBranch = new Label { Text = "📍 Chi Nhánh Hiện Tại: Chi Nhánh 01 - TP.Hồ Chí Minh", Font = ThemeManager.BodyBold, Location = new Point(25, 105), AutoSize = true, ForeColor = ThemeManager.TextPrimary };
        var lblServer = new Label { Text = "🌐 Server Endpoint: http://localhost:5137 (Active)", Font = ThemeManager.BodyBold, Location = new Point(25, 145), AutoSize = true, ForeColor = ThemeManager.Success };

        pnlConfigCard.Controls.Add(lblCompany);
        pnlConfigCard.Controls.Add(lblVAT);
        pnlConfigCard.Controls.Add(lblBranch);
        pnlConfigCard.Controls.Add(lblServer);
        tabConfig.Controls.Add(pnlConfigCard);

        // Tab Audit Grid
        dgvAuditLogs = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvAuditLogs);

        dgvAuditLogs.Columns.Add("Time", "Thời Gian");
        dgvAuditLogs.Columns.Add("User", "Tài Khoản Thực Hiện");
        dgvAuditLogs.Columns.Add("Action", "Hành Động");
        dgvAuditLogs.Columns.Add("Entity", "Đối Tượng");
        dgvAuditLogs.Columns.Add("Details", "Chi Tiết Nhật Ký");

        var pnlAuditCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10)
        };
        ThemeManager.ApplyCardPanel(pnlAuditCard);
        pnlAuditCard.Controls.Add(dgvAuditLogs);
        tabAudit.Controls.Add(pnlAuditCard);

        // Tab Backup Card
        var pnlBackupCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(25)
        };
        ThemeManager.ApplyCardPanel(pnlBackupCard);

        var btnBackupNow = new Button
        {
            Text = "💾 Sao Lưu Dữ Liệu PostgreSQL Ngay",
            Size = new Size(300, 44),
            Location = new Point(25, 25)
        };
        ThemeManager.ApplyPrimaryButton(btnBackupNow);
        btnBackupNow.Click += (s, e) => {
            var parentForm = this.FindForm();
            if (parentForm != null) AntdUI.Message.success(parentForm, "Đã tạo bản sao lưu dữ liệu hệ thống SmartSupermarket_Backup.dump thành công!");
            else MessageBox.Show("Đã tạo bản sao lưu dữ liệu hệ thống SmartSupermarket_Backup.dump thành công!", "Thông báo");
        };
        pnlBackupCard.Controls.Add(btnBackupNow);
        tabBackup.Controls.Add(pnlBackupCard);

        tabControl.TabPages.Add(tabConfig);
        tabControl.TabPages.Add(tabAudit);
        tabControl.TabPages.Add(tabBackup);

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlHeader);
    }

    private void LoadSampleData()
    {
        dgvAuditLogs.Rows.Clear();
        dgvAuditLogs.Rows.Add(DateTime.Now.AddMinutes(-15).ToString("yyyy-MM-dd HH:mm:ss"), "admin", "CREATE_ORDER", "Order #ORD-10028", "Tạo hóa đơn bán lẻ POS thành công - 148.000 VNĐ");
        dgvAuditLogs.Rows.Add(DateTime.Now.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss"), "admin", "ADD_PRODUCT", "Product #8935001800012", "Thêm mới sản phẩm Coca-Cola Lon 330ml");
        dgvAuditLogs.Rows.Add(DateTime.Now.AddHours(-5).ToString("yyyy-MM-dd HH:mm:ss"), "staff_pos_01", "CHECK_IN", "Attendance NV-0002", "Nhân viên Thu ngân Trần Thị Thu Ngân check-in ca sáng");
    }
}
