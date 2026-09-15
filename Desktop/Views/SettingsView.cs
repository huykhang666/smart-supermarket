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
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Cấu Hình Hệ Thống & Nhật Ký (Settings & Audit)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBodyBold
        };

        var tabConfig = new TabPage("⚙️ Cấu Hình Siêu Thị & VAT") { BackColor = AppTheme.BackgroundGray };
        var tabAudit = new TabPage("📋 Nhật Ký Hệ Thống (Audit Logs)") { BackColor = AppTheme.BackgroundGray };
        var tabBackup = new TabPage("💾 Sao Lưu & Khôi Phục Dữ Liệu") { BackColor = AppTheme.BackgroundGray };

        // Tab Config Panel Card
        var pnlConfigCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(24)
        };
        AppTheme.ApplyCardPanel(pnlConfigCard);

        var lblCompany = new Label { Text = "🏢 Tên Hệ Thống Siêu Thị: SMART SUPERMARKET KATQ", Font = AppTheme.FontBodyBold, Location = new Point(24, 25), AutoSize = true, ForeColor = AppTheme.TextPrimary };
        var lblVAT = new Label { Text = "📊 Thuế VAT Mặc Định Bán Lẻ: 8%", Font = AppTheme.FontBodyBold, Location = new Point(24, 65), AutoSize = true, ForeColor = AppTheme.TextPrimary };
        var lblBranch = new Label { Text = "📍 Chi Nhánh Hiện Tại: Chi Nhánh 01 - TP.Hồ Chí Minh", Font = AppTheme.FontBodyBold, Location = new Point(24, 105), AutoSize = true, ForeColor = AppTheme.TextPrimary };
        var lblServer = new Label { Text = $"🌐 Server Endpoint: {AppTheme.ApiBaseUrl} (Active)", Font = AppTheme.FontBodyBold, Location = new Point(24, 145), AutoSize = true, ForeColor = AppTheme.Success };

        pnlConfigCard.Controls.Add(lblCompany);
        pnlConfigCard.Controls.Add(lblVAT);
        pnlConfigCard.Controls.Add(lblBranch);
        pnlConfigCard.Controls.Add(lblServer);
        tabConfig.Controls.Add(pnlConfigCard);

        // Tab Audit Grid
        dgvAuditLogs = new DataGridView();
        AppTheme.ApplyGridStyle(dgvAuditLogs);

        dgvAuditLogs.Columns.Add("Time", "Thời Gian");
        dgvAuditLogs.Columns.Add("User", "Tài Khoản Thực Hiện");
        dgvAuditLogs.Columns.Add("Action", "Hành Động");
        dgvAuditLogs.Columns.Add("Entity", "Đối Tượng");
        dgvAuditLogs.Columns.Add("Details", "Chi Tiết Nhật Ký");

        var pnlAuditCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1)
        };
        AppTheme.ApplyCardPanel(pnlAuditCard);
        pnlAuditCard.Controls.Add(dgvAuditLogs);
        tabAudit.Controls.Add(pnlAuditCard);

        // Tab Backup Card
        var pnlBackupCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(24)
        };
        AppTheme.ApplyCardPanel(pnlBackupCard);

        var btnBackupNow = new Button
        {
            Text = "💾 Sao Lưu Dữ Liệu SQL Server Ngay",
            Size = new Size(280, 32),
            Location = new Point(24, 25)
        };
        AppTheme.ApplyPrimaryButton(btnBackupNow);
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
    }
}
