using System;
using System.Drawing;
using System.Windows.Forms;

namespace Desktop.Views;

public class AiCopilotView : UserControl
{
    private RichTextBox rtbChatLog = null!;
    private TextBox txtPrompt = null!;
    private Button btnSend = null!;

    public AiCopilotView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(20, 15, 20, 15),
            Margin = new Padding(0, 0, 0, 15)
        };
        ThemeManager.ApplyCardPanel(pnlHeader);

        var lblTitle = new Label
        {
            Text = "🤖 TRUNG TÂM TRỢ LÝ AI & ĐIỀU HÀNH DỮ LIỆU CHUỖI SIÊU THỊ",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            AutoSize = true,
            Location = new Point(20, 18)
        };

        pnlHeader.Controls.Add(lblTitle);

        var pnlBottomInput = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 15, 0, 0)
        };
        ThemeManager.ApplyCardPanel(pnlBottomInput);

        txtPrompt = new TextBox
        {
            PlaceholderText = "💬 Nhập câu hỏi nghiệp vụ (Ví dụ: 'Mặt hàng nào có tỷ suất sinh lời cao nhất?', 'Dự báo doanh thu tuần tới')...",
            Font = ThemeManager.BodyFont,
            Size = new Size(780, 36),
            Location = new Point(15, 16),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BorderStyle = BorderStyle.FixedSingle
        };
        txtPrompt.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnSend_Click(s, e); };

        btnSend = new Button
        {
            Text = "GỬI (ENTER)",
            Size = new Size(140, 36),
            Location = new Point(pnlBottomInput.Width - 160, 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.ApplyPrimaryButton(btnSend);
        btnSend.Click += BtnSend_Click;

        pnlBottomInput.Controls.Add(txtPrompt);
        pnlBottomInput.Controls.Add(btnSend);

        var pnlChatContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15)
        };
        ThemeManager.ApplyCardPanel(pnlChatContainer);

        rtbChatLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Font = ThemeManager.BodyFont,
            Padding = new Padding(15)
        };

        rtbChatLog.AppendText("🤖 Trợ lý Smart Supermarket AI Assistant sẵn sàng trợ giúp!\n\n");
        rtbChatLog.AppendText("• AI Forecast: Doanh thu dự kiến tuần tới đạt 1.62 Tỷ VNĐ (+12% so với tuần trước).\n");
        rtbChatLog.AppendText("• AI Recommendation: Khuyến nghị nhập thêm 250 lốc Coca-Cola Lon 330ml cho chi nhánh Quận 7.\n");
        rtbChatLog.AppendText("• Dynamic Pricing: Đã tự động đề xuất giảm 15% cho 3 mặt hàng cận HSD 7 ngày để tối ưu vòng quay tồn kho.\n\n");

        pnlChatContainer.Controls.Add(rtbChatLog);

        this.Controls.Add(pnlChatContainer);
        this.Controls.Add(pnlBottomInput);
        this.Controls.Add(pnlHeader);
    }

    private void BtnSend_Click(object? sender, EventArgs e)
    {
        string text = txtPrompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        rtbChatLog.AppendText($"👤 Admin: {text}\n");
        rtbChatLog.AppendText($"🤖 AI Copilot: Đang phân tích truy vấn dữ liệu '{text}'...\n");
        rtbChatLog.AppendText("-> Kết quả: Nhóm hàng Nước Giải Khát & Chế Phẩm Sữa đóng góp 34.2% tổng lợi nhuận hệ thống!\n\n");

        txtPrompt.Clear();
    }
}
