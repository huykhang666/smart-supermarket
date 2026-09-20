using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class StaffNotificationsView : UserControl
{
    private FlowLayoutPanel pnlNotificationList = null!;

    public StaffNotificationsView()
    {
        InitializeComponent();
        LoadNotifications();
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
            Text = "📢 Trung Tâm Thông Báo & Cảnh Báo Nhân Viên",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Cập nhật realtime các sự kiện phát sinh: Lô hàng nhập, cảnh báo hết hàng, đơn online và hạn dùng",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        var pnlHeaderRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        var btnClear = new Button
        {
            Text = "✓ Đã Đọc Tất Cả",
            Size = new Size(160, 32)
        };
        AppTheme.ApplyPrimaryButton(btnClear);
        btnClear.Click += (s, e) =>
        {
            pnlNotificationList.Controls.Clear();
            var lblEmpty = new Label
            {
                Text = "🎉 Tuyệt vời! Hiện tại bạn đã xem hết toàn bộ thông báo hệ thống.",
                Font = AppTheme.FontBodyBold,
                ForeColor = AppTheme.Success,
                Padding = new Padding(20),
                AutoSize = true
            };
            pnlNotificationList.Controls.Add(lblEmpty);
            AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã đánh dấu đã đọc tất cả thông báo.");
        };
        pnlHeaderRight.Controls.Add(btnClear);

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);
        pnlHeader.Controls.Add(pnlHeaderRight);

        // --- 2. Notification List Container ---
        var pnlContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16),
            Margin = new Padding(0, 10, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlContainer);

        pnlNotificationList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlNotificationList.Resize += (s, e) =>
        {
            int targetWidth = Math.Max(300, pnlNotificationList.ClientSize.Width - 25);
            foreach (Control c in pnlNotificationList.Controls)
            {
                if (c is Panel p) p.Width = targetWidth;
            }
        };

        pnlContainer.Controls.Add(pnlNotificationList);

        this.Controls.Add(pnlContainer);
        this.Controls.Add(pnlHeader);
    }

    private void LoadNotifications()
    {
        pnlNotificationList.Controls.Clear();
        var lblEmpty = new Label
        {
            Text = "🎉 Tuyệt vời! Hiện tại bạn không có thông báo cảnh báo nào chưa đọc.",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.Success,
            Padding = new Padding(20),
            AutoSize = true
        };
        pnlNotificationList.Controls.Add(lblEmpty);
    }

    private Panel CreateNotificationItem(string title, string content, string time, Color accent, IconChar icon)
    {
        int cardWidth = Math.Max(300, pnlNotificationList.ClientSize.Width - 25);
        var card = new Panel
        {
            Size = new Size(cardWidth, 80),
            BackColor = AppTheme.BackgroundGray,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(12)
        };
        card.Paint += (s, e) =>
        {
            using var b = new SolidBrush(accent);
            e.Graphics.FillRectangle(b, 0, 0, 4, card.Height);
        };

        var picIcon = new IconPictureBox
        {
            IconChar = icon,
            IconColor = accent,
            IconSize = 24,
            Size = new Size(24, 24),
            Location = new Point(14, 14),
            BackColor = Color.Transparent
        };

        var lblT = new Label
        {
            Text = title,
            Font = AppTheme.FontBodyBold,
            ForeColor = accent,
            Location = new Point(48, 12),
            AutoSize = true
        };

        var lblTime = new Label
        {
            Text = time,
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(card.Width - 120, 12),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var lblC = new Label
        {
            Text = content,
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(48, 38),
            Size = new Size(card.Width - 70, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        card.Controls.Add(picIcon);
        card.Controls.Add(lblT);
        card.Controls.Add(lblTime);
        card.Controls.Add(lblC);
        return card;
    }
}
