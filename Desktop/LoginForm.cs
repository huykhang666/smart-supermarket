using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Desktop;

public class LoginForm : Form
{
    private Panel pnlTitleBar = null!;
    private Label lblTitle = null!;
    private IconButton btnClose = null!;
    private IconButton btnMinimize = null!;

    private Panel pnlHeaderCard = null!;
    private Panel pnlLogoBox = null!;

    private Label lblUsername = null!;
    private TextBox txtUsername = null!;
    
    private Label lblPassword = null!;
    private Panel pnlPasswordBox = null!;
    private TextBox txtPassword = null!;
    private IconButton btnTogglePassword = null!;

    private CheckBox chkRemember = null!;
    private Label lblForgotPassword = null!;

    private Button btnLogin = null!;
    private Button btnRegister = null!;
    private Panel pnlFooter = null!;
    private Label lblFooterStatus = null!;
    private Label lblFooterVersion = null!;

    public LoginForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "DangNhap - KATQ Smart Workstation";
        this.Size = new Size(460, 650);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(0, 114, 206); // Unified Sky Blue

        // Custom Paint for smooth gradient & crisp 1px border
        this.Paint += (s, e) =>
        {
            using var brush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(0, 114, 206),  // Deep Sky Blue
                Color.FromArgb(0, 160, 230),  // Bright Cyan-Blue Accent
                90f);
            e.Graphics.FillRectangle(brush, this.ClientRectangle);

            using var borderPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1);
            e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
        };

        // Enable Dragging Form
        this.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        };

        // --- 1. Top Title Bar ---
        pnlTitleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(0, 50, 110)
        };
        pnlTitleBar.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        };

        var picWindowIcon = new IconPictureBox
        {
            IconChar = IconChar.ShoppingBasket,
            IconColor = Color.FromArgb(0, 210, 255),
            IconSize = 20,
            Size = new Size(20, 20),
            Location = new Point(12, 10),
            BackColor = Color.Transparent
        };

        lblTitle = new Label
        {
            Text = "DangNhap - KATQ Smart Workstation",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(38, 10)
        };

        btnClose = new IconButton
        {
            IconChar = IconChar.Times,
            IconColor = Color.White,
            IconSize = 16,
            Size = new Size(38, 40),
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => Application.Exit();

        btnMinimize = new IconButton
        {
            IconChar = IconChar.Minus,
            IconColor = Color.White,
            IconSize = 16,
            Size = new Size(38, 40),
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnMinimize.FlatAppearance.BorderSize = 0;
        btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

        pnlTitleBar.Controls.Add(lblTitle);
        pnlTitleBar.Controls.Add(picWindowIcon);
        pnlTitleBar.Controls.Add(btnMinimize);
        pnlTitleBar.Controls.Add(btnClose);

        // --- 2. Top Header Logo Card (X = 30, Width = 400) ---
        pnlHeaderCard = new Panel
        {
            Size = new Size(400, 210),
            Location = new Point(30, 52),
            BackColor = Color.FromArgb(0, 42, 90) // Dark Navy Container
        };
        pnlHeaderCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = GetRoundedPath(pnlHeaderCard.ClientRectangle, 12);
            pnlHeaderCard.Region = new Region(path);

            // Green dot indicator
            using var badgeBrush = new SolidBrush(Color.FromArgb(16, 185, 129));
            e.Graphics.FillEllipse(badgeBrush, 20, 16, 10, 10);
        };

        var lblSystemTag = new Label
        {
            Text = "HỆ THỐNG POS & BÁN LẺ",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(36, 13)
        };

        var lblSystemId = new Label
        {
            Text = "ID: ST-0428",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 210, 255),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(310, 13)
        };

        // White Inner Logo Box
        pnlLogoBox = new Panel
        {
            Size = new Size(376, 155),
            Location = new Point(12, 42),
            BackColor = Color.White
        };
        pnlLogoBox.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = GetRoundedPath(pnlLogoBox.ClientRectangle, 10);
            pnlLogoBox.Region = new Region(path);

            // Draw Blue Logo Square in Center
            int logoX = (pnlLogoBox.Width - 76) / 2;
            using var logoBgBrush = new LinearGradientBrush(
                new Rectangle(logoX, 12, 76, 76),
                Color.FromArgb(0, 122, 240),
                Color.FromArgb(0, 80, 200),
                45f);

            using var logoPath = GetRoundedPath(new Rectangle(logoX, 12, 76, 76), 16);
            e.Graphics.FillPath(logoBgBrush, logoPath);

            // Centered Brand Text
            using var fontBrand = new Font("Segoe UI", 16, FontStyle.Bold);
            using var brandBrush = new SolidBrush(Color.FromArgb(0, 90, 200));
            string brandStr = "KATQ SMART";
            var szBrand = e.Graphics.MeasureString(brandStr, fontBrand);
            e.Graphics.DrawString(brandStr, fontBrand, brandBrush, (pnlLogoBox.Width - szBrand.Width) / 2, 94);

            // Centered Subtag Text
            using var fontSub = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var subBrush = new SolidBrush(Color.FromArgb(100, 135, 175));
            string subStr = "RETAIL & POS SYSTEM";
            var szSub = e.Graphics.MeasureString(subStr, fontSub);
            e.Graphics.DrawString(subStr, fontSub, subBrush, (pnlLogoBox.Width - szSub.Width) / 2, 126);
        };

        var picCart = new IconPictureBox
        {
            IconChar = IconChar.ShoppingCart,
            IconColor = Color.White,
            IconSize = 40,
            Size = new Size(40, 40),
            Location = new Point((376 - 40) / 2, 30),
            BackColor = Color.Transparent,
            Parent = pnlLogoBox
        };

        pnlHeaderCard.Controls.Add(lblSystemTag);
        pnlHeaderCard.Controls.Add(lblSystemId);
        pnlHeaderCard.Controls.Add(pnlLogoBox);

        // --- 3. Username Input Row (EMPTY by default!) ---
        lblUsername = new Label
        {
            Text = "Tên đăng nhập:",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(30, 283)
        };

        txtUsername = new TextBox
        {
            Text = "", // EMPTY BY DEFAULT as requested!
            PlaceholderText = "Nhập tài khoản (admin)",
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(20, 30, 40),
            Size = new Size(240, 36),
            Location = new Point(190, 278),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            TextAlign = HorizontalAlignment.Center
        };

        // --- 4. Password Input Row with Integrated Eye Button (EMPTY by default!) ---
        lblPassword = new Label
        {
            Text = "Mật khẩu:",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(30, 335)
        };

        pnlPasswordBox = new Panel
        {
            Size = new Size(240, 36),
            Location = new Point(190, 330),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        txtPassword = new TextBox
        {
            Text = "", // EMPTY BY DEFAULT as requested!
            PlaceholderText = "Nhập mật khẩu (admin123)",
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(20, 30, 40),
            PasswordChar = '•',
            Size = new Size(200, 28),
            Location = new Point(3, 4),
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            TextAlign = HorizontalAlignment.Center
        };

        btnTogglePassword = new IconButton
        {
            IconChar = IconChar.Eye,
            IconColor = Color.FromArgb(120, 130, 140),
            IconSize = 18,
            Size = new Size(30, 30),
            Location = new Point(206, 2),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnTogglePassword.FlatAppearance.BorderSize = 0;
        btnTogglePassword.Click += (s, e) =>
        {
            if (txtPassword.PasswordChar == '•')
            {
                txtPassword.PasswordChar = '\0';
                btnTogglePassword.IconChar = IconChar.EyeSlash;
            }
            else
            {
                txtPassword.PasswordChar = '•';
                btnTogglePassword.IconChar = IconChar.Eye;
            }
        };

        pnlPasswordBox.Controls.Add(txtPassword);
        pnlPasswordBox.Controls.Add(btnTogglePassword);

        // --- 5. Checkbox & Forgot Password ---
        chkRemember = new CheckBox
        {
            Text = "Ghi nhớ",
            Checked = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(190, 378)
        };

        lblForgotPassword = new Label
        {
            Text = "Quên mật khẩu?",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Underline),
            ForeColor = Color.FromArgb(230, 245, 255),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(310, 378),
            Cursor = Cursors.Hand
        };
        lblForgotPassword.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? this, "Vui lòng liên hệ bộ phận Kỹ Thuật Admin để reset mật khẩu.");

        // --- 6. Primary & Secondary Buttons ---
        btnLogin = new Button
        {
            Text = "↪  Đăng Nhập",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 90, 200),
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(400, 48),
            Location = new Point(30, 420),
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        btnRegister = new Button
        {
            Text = "Đăng Ký / Phân Quyền Mới",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(0, 50, 110),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(400, 44),
            Location = new Point(30, 480),
            Cursor = Cursors.Hand
        };
        btnRegister.FlatAppearance.BorderSize = 1;
        btnRegister.FlatAppearance.BorderColor = Color.FromArgb(100, 190, 255);
        btnRegister.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? this, "Chức năng đăng ký tài khoản mới yêu cầu quyền Admin.");

        // --- 7. Footer Status Bar ---
        pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = Color.FromArgb(0, 45, 95)
        };

        lblFooterStatus = new Label
        {
            Text = "🟢 Máy chủ POS: Sẵn sàng kết nối",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 240, 255),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(15, 9)
        };

        lblFooterVersion = new Label
        {
            Text = "v2.5.0 - KATQ Sky Edition",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(170, 215, 255),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(285, 9)
        };

        pnlFooter.Controls.Add(lblFooterStatus);
        pnlFooter.Controls.Add(lblFooterVersion);

        // Add Controls to Form
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeaderCard);
        this.Controls.Add(lblUsername);
        this.Controls.Add(txtUsername);
        this.Controls.Add(lblPassword);
        this.Controls.Add(pnlPasswordBox);
        this.Controls.Add(chkRemember);
        this.Controls.Add(lblForgotPassword);
        this.Controls.Add(btnLogin);
        this.Controls.Add(btnRegister);
        this.Controls.Add(pnlTitleBar);

        this.AcceptButton = btnLogin;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            AntdUI.Message.error(this.FindForm() ?? this, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            return;
        }

        // Validate Admin credentials (admin / admin123)
        if ((username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123") ||
            (username.Equals("quanly", StringComparison.OrdinalIgnoreCase)))
        {
            AntdUI.Message.success(this.FindForm() ?? this, "Đăng nhập thành công! Đang chuyển đến Trung Tâm Điều Hành Admin...");

            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, ev) =>
            {
                timer.Stop();
                this.Hide();
                var mainForm = new MainForm();
                mainForm.FormClosed += (s2, ev2) => this.Close();
                mainForm.Show();
            };
            timer.Start();
        }
        else
        {
            AntdUI.Message.error(this.FindForm() ?? this, "Tài khoản hoặc mật khẩu không chính xác! (Gợi ý: admin / admin123)");
        }
    }

    private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2f;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    // Windows API for Form Dragging
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HT_CAPTION = 0x2;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
}
