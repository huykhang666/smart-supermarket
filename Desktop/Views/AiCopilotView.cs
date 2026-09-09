using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

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
        this.BackColor = Color.FromArgb(240, 242, 245);
        this.Padding = new Padding(20);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(230, 244, 255),
            Padding = new Padding(20, 15, 20, 15)
        };

        var lblTitle = new Label
        {
            Text = "🤖 TRUNG TÂM TRỢ LÝ AI & ĐIỀU HÀNH DỮ LIỆU CHUỖI SIÊU THỊ",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(9, 109, 217),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        pnlHeader.Controls.Add(lblTitle);

        var pnlBottomInput = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        txtPrompt = new TextBox
        {
            PlaceholderText = "💬 Nhập câu hỏi nghiệp vụ (Ví dụ: 'Mặt hàng nào có tỷ suất sinh lời cao nhất?', 'Dự báo doanh thu tuần tới')...",
            Font = new Font("Segoe UI", 10.5f),
            Size = new Size(800, 36),
            Location = new Point(15, 16),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnSend = new Button
        {
            Text = "GỬI (ENTER)",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(9, 109, 217),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 36),
            Location = new Point(pnlBottomInput.Width - 150, 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnSend.FlatAppearance.BorderSize = 0;
        btnSend.Click += BtnSend_Click;

        pnlBottomInput.Controls.Add(txtPrompt);
        pnlBottomInput.Controls.Add(btnSend);

        rtbChatLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Font = new Font("Segoe UI", 10.5f),
            Padding = new Padding(20)
        };

        rtbChatLog.AppendText("🤖 Trợ lý Smart Supermarket AI (v2.5) sẵn sàng trợ giúp!\n\n");
        rtbChatLog.AppendText("• AI Forecast: Doanh thu dự kiến tuần tới đạt 1.62 Tỷ VNĐ (+12% so với tuần trước).\n");
        rtbChatLog.AppendText("• AI Recommendation: Khuyến nghị nhập thêm 250 lốc Pepsi Lon 330ml cho chi nhánh Thủ Đức.\n");
        rtbChatLog.AppendText("• Dynamic Pricing: Đã hạ giá tự động 4 mặt hàng cận HSD 7 ngày để tối ưu giải phóng vốn.\n\n");

        this.Controls.Add(rtbChatLog);
        this.Controls.Add(pnlBottomInput);
        this.Controls.Add(pnlHeader);
    }

    private void BtnSend_Click(object? sender, EventArgs e)
    {
        string text = txtPrompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        rtbChatLog.AppendText($"👤 Admin: {text}\n");
        rtbChatLog.AppendText($"🤖 AI Copilot: Đang phân tích truy vấn dữ liệu '{text}'...\n");
        rtbChatLog.AppendText("-> Kết quả: Nhóm hàng Nước Giải Khát & Bánh Kẹo hiện đóng góp 28.4% tổng biên lợi nhuận toàn hệ thống!\n\n");

        txtPrompt.Clear();
    }
}
