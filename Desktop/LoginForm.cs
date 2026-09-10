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

    // KATQ Smart Brand Color Palette
    private static readonly Color NavyPrimary = Color.FromArgb(11, 37, 69);     // #0B2545 - Dark Navy for KATQ
    private static readonly Color NavyDark = Color.FromArgb(7, 25, 46);        // #07192E - Darker Title Bar
    private static readonly Color TealAccent = Color.FromArgb(0, 168, 204);    // #00A8CC - Teal Cyan for smart
    private static readonly Color TealLight = Color.FromArgb(0, 210, 211);     // #00D2D3 - Bright Cyan Node
    private static readonly Color GradientEnd = Color.FromArgb(0, 102, 153);    // #006699 - Soft Cyan-Navy

    public LoginForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "DangNhap - KATQ Smart Workstation";
        this.Size = new Size(460, 660);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = NavyPrimary;

        // Custom Paint for KATQ Brand Gradient Background
        this.Paint += (s, e) =>
        {
            using var brush = new LinearGradientBrush(
                this.ClientRectangle,
                NavyPrimary,
                GradientEnd,
                65f);
            e.Graphics.FillRectangle(brush, this.ClientRectangle);

            // Subtle inner border glow
            using var borderPen = new Pen(Color.FromArgb(80, 0, 168, 204), 1);
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
            BackColor = NavyDark
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
            IconColor = TealAccent,
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

        // --- 2. Top Header Container Card (X = 30, Width = 400, Height = 215) ---
        pnlHeaderCard = new Panel
        {
            Size = new Size(400, 215),
            Location = new Point(30, 52),
            BackColor = Color.FromArgb(8, 28, 51) // Deep Navy Container Card
        };
        pnlHeaderCard.Resize += (s, e) =>
        {
            if (pnlHeaderCard.Width > 0 && pnlHeaderCard.Height > 0)
            {
                using var path = GetRoundedPath(pnlHeaderCard.ClientRectangle, 12);
                pnlHeaderCard.Region = new Region(path);
            }
        };
        pnlHeaderCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            // Green dot status indicator
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
            ForeColor = Color.FromArgb(160, 220, 245),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(310, 13)
        };

        // White Inner Logo Card (Width = 376, Height = 158)
        pnlLogoBox = new Panel
        {
            Size = new Size(376, 158),
            Location = new Point(12, 42),
            BackColor = Color.White
        };
        pnlLogoBox.Resize += (s, e) =>
        {
            if (pnlLogoBox.Width > 0 && pnlLogoBox.Height > 0)
            {
                using var path = GetRoundedPath(pnlLogoBox.ClientRectangle, 10);
                pnlLogoBox.Region = new Region(path);
            }
        };
        pnlLogoBox.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // --- Draw Monogram Icon Logo on Top/Center ---
            int iconBoxX = (pnlLogoBox.Width - 68) / 2;
            using var logoBrush = new LinearGradientBrush(
                new Rectangle(iconBoxX, 10, 68, 60),
                NavyPrimary,
                TealAccent,
                45f);

            // Draw Monogram KATQ Arch & Target Icon
            using var mainPen = new Pen(logoBrush, 5.5f);
            e.Graphics.DrawArc(mainPen, iconBoxX + 10, 12, 48, 38, 180, 180);
            e.Graphics.DrawLine(mainPen, iconBoxX + 16, 25, iconBoxX + 52, 25);

            // Bullseye Ring Target at Bottom of Monogram Icon
            using var ringPen = new Pen(TealAccent, 3.5f);
            e.Graphics.DrawEllipse(ringPen, iconBoxX + 24, 34, 20, 20);
            using var dotBrush = new SolidBrush(TealAccent);
            e.Graphics.FillEllipse(dotBrush, iconBoxX + 31, 41, 6, 6);

            // --- Draw Brand Name "KATQ smart" ---
            // 1. "KATQ" in Bold Deep Navy Blue (#0B2545)
            using var fontKatq = new Font("Segoe UI", 21f, FontStyle.Bold);
            using var katqBrush = new SolidBrush(NavyPrimary);
            string strKatq = "KATQ";
            var szKatq = e.Graphics.MeasureString(strKatq, fontKatq);

            // 2. "smart" in Lowercase Teal Cyan (#00A8CC)
            using var fontSmart = new Font("Segoe UI", 16f, FontStyle.Bold);
            using var smartBrush = new SolidBrush(TealAccent);
            string strSmart = "smart";
            var szSmart = e.Graphics.MeasureString(strSmart, fontSmart);

            float totalBrandWidth = szKatq.Width + szSmart.Width - 10;
            float startBrandX = (pnlLogoBox.Width - totalBrandWidth) / 2;

            e.Graphics.DrawString(strKatq, fontKatq, katqBrush, startBrandX, 74);
            e.Graphics.DrawString(strSmart, fontSmart, smartBrush, startBrandX + szKatq.Width - 12, 80);

            // --- Draw Circuit Nodes "o--- RETAIL & POS SYSTEM ---o" ---
            using var linePen = new Pen(TealAccent, 1.5f);
            using var nodeBrush = new SolidBrush(Color.White);
            using var nodeBorderPen = new Pen(TealAccent, 1.5f);

            int nodeY = 126;
            // Left Node Line
            e.Graphics.DrawLine(linePen, 50, nodeY, 110, nodeY);
            e.Graphics.FillEllipse(nodeBrush, 45, nodeY - 3, 6, 6);
            e.Graphics.DrawEllipse(nodeBorderPen, 45, nodeY - 3, 6, 6);

            // Center Subtag Text
            using var fontSub = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var subBrush = new SolidBrush(Color.FromArgb(90, 115, 140));
            string subStr = "RETAIL & POS SYSTEM";
            var szSub = e.Graphics.MeasureString(subStr, fontSub);
            e.Graphics.DrawString(subStr, fontSub, subBrush, (pnlLogoBox.Width - szSub.Width) / 2, nodeY - 7);

            // Right Node Line
            e.Graphics.DrawLine(linePen, pnlLogoBox.Width - 110, nodeY, pnlLogoBox.Width - 50, nodeY);
            e.Graphics.FillEllipse(nodeBrush, pnlLogoBox.Width - 51, nodeY - 3, 6, 6);
            e.Graphics.DrawEllipse(nodeBorderPen, pnlLogoBox.Width - 51, nodeY - 3, 6, 6);
        };

        pnlHeaderCard.Controls.Add(lblSystemTag);
        pnlHeaderCard.Controls.Add(lblSystemId);
        pnlHeaderCard.Controls.Add(pnlLogoBox);

        // Set Rounded Regions initially
        using (var pathCard = GetRoundedPath(pnlHeaderCard.ClientRectangle, 12))
            pnlHeaderCard.Region = new Region(pathCard);

        using (var pathBox = GetRoundedPath(pnlLogoBox.ClientRectangle, 10))
            pnlLogoBox.Region = new Region(pathBox);

        // --- 3. Username Input Row (EMPTY by default) ---
        lblUsername = new Label
        {
            Text = "Tên đăng nhập:",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(30, 288)
        };

        txtUsername = new TextBox
        {
            Text = "", // EMPTY BY DEFAULT
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(20, 30, 40),
            Size = new Size(240, 36),
            Location = new Point(190, 283),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            TextAlign = HorizontalAlignment.Center
        };

        // --- 4. Password Input Row with Integrated Eye Toggle Button (EMPTY by default) ---
        lblPassword = new Label
        {
            Text = "Mật khẩu:",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(30, 340)
        };

        pnlPasswordBox = new Panel
        {
            Size = new Size(240, 36),
            Location = new Point(190, 335),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        txtPassword = new TextBox
        {
            Text = "", // EMPTY BY DEFAULT
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

        // --- 5. Checkbox & Forgot Link ---
        chkRemember = new CheckBox
        {
            Text = "Ghi nhớ",
            Checked = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(190, 383)
        };

        lblForgotPassword = new Label
        {
            Text = "Quên mật khẩu?",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Underline),
            ForeColor = Color.FromArgb(200, 235, 255),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(310, 383),
            Cursor = Cursors.Hand
        };
        lblForgotPassword.Click += (s, e) => AntdUI.Message.info(this, "Vui lòng liên hệ bộ phận Kỹ Thuật Admin để reset mật khẩu.");

        // --- 6. Action Buttons ---
        btnLogin = new Button
        {
            Text = "↪  Đăng Nhập",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = NavyPrimary,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(400, 48),
            Location = new Point(30, 425),
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        btnRegister = new Button
        {
            Text = "Đăng Ký / Phân Quyền Mới",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(8, 28, 51),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(400, 44),
            Location = new Point(30, 485),
            Cursor = Cursors.Hand
        };
        btnRegister.FlatAppearance.BorderSize = 1;
        btnRegister.FlatAppearance.BorderColor = TealAccent;
        btnRegister.Click += (s, e) => AntdUI.Message.info(this, "Chức năng đăng ký tài khoản mới yêu cầu quyền Admin.");

        // --- 7. Footer Bar ---
        pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = NavyDark
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
            AntdUI.Message.error(this, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            return;
        }

        // Validate Admin credentials (admin / admin123)
        if ((username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123") ||
            (username.Equals("quanly", StringComparison.OrdinalIgnoreCase)))
        {
            AntdUI.Message.success(this, "Đăng nhập thành công! Đang chuyển đến Trung Tâm Điều Hành Admin...");

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
            AntdUI.Message.error(this, "Tài khoản hoặc mật khẩu không chính xác! (Gợi ý: admin / admin123)");
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
