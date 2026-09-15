using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop.Views;

public class AiCopilotView : UserControl
{
    private FlowLayoutPanel pnlRecommendations = null!;

    public AiCopilotView()
    {
        InitializeComponent();
        LoadAiInsights();
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
            Text = "🤖 Trợ Lý Trí Tuệ Nhân Tạo (AI Assistant Co-Pilot)",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Đưa ra khuyến nghị & phân tích hành động thực tế dựa trên dữ liệu bán hàng, tồn kho và chu kỳ mua sắm",
            Font = AppTheme.FontCaption,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 34),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // --- 2. Recommendations List Container ---
        var pnlContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16),
            Margin = new Padding(0, 8, 0, 0)
        };
        AppTheme.ApplyCardPanel(pnlContainer);

        pnlRecommendations = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        pnlContainer.Controls.Add(pnlRecommendations);

        this.Controls.Add(pnlContainer);
        this.Controls.Add(pnlHeader);
    }

    private void LoadAiInsights()
    {
        pnlRecommendations.Controls.Clear();

        var lblEmpty = new Label
        {
            Text = "✨ Trợ lý AI đang phân tích dữ liệu siêu thị theo thời gian thực...\nHiện tại không có bất thường nào về tồn kho, hạn sử dụng hoặc đối soát đơn hàng cần xử lý gấp.",
            Font = AppTheme.FontBodyBold,
            ForeColor = AppTheme.Primary,
            Padding = new Padding(20),
            AutoSize = true
        };
        pnlRecommendations.Controls.Add(lblEmpty);
    }

    private Panel CreateInsightCard(string title, string content, string actionText, Color accent, IconChar icon, Action onAction)
    {
        var card = new Panel
        {
            Size = new Size(1020, 110),
            BackColor = AppTheme.BackgroundGray,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(16)
        };
        card.Paint += (s, e) =>
        {
            using var b = new SolidBrush(accent);
            e.Graphics.FillRectangle(b, 0, 0, 4, card.Height);
        };

        var pic = new IconPictureBox
        {
            IconChar = icon,
            IconColor = accent,
            IconSize = 28,
            Size = new Size(28, 28),
            Location = new Point(14, 16),
            BackColor = Color.Transparent
        };

        var lblT = new Label
        {
            Text = title,
            Font = AppTheme.FontBodyBold,
            ForeColor = accent,
            Location = new Point(50, 14),
            AutoSize = true
        };

        var lblC = new Label
        {
            Text = content,
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(50, 42),
            Size = new Size(card.Width - 270, 52),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var btnAction = new Button
        {
            Text = actionText,
            Font = AppTheme.FontBodyBold,
            Size = new Size(185, 34),
            Location = new Point(card.Width - 200, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        AppTheme.ApplyPrimaryButton(btnAction);
        btnAction.Click += (s, e) => onAction();

        card.Controls.Add(pic);
        card.Controls.Add(lblT);
        card.Controls.Add(lblC);
        card.Controls.Add(btnAction);

        return card;
    }
}
