using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class PromotionsView : UserControl
{
    private DataGridView dgvPromotions = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private Button btnDelete = null!;
    private Label lblStatus = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5137";

    public PromotionsView()
    {
        InitializeComponent();
        _ = LoadPromotionsAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        // Header
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

        // Toolbar
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10, 8, 10, 8)
        };
        ThemeManager.ApplyCardPanel(pnlToolbar);

        btnRefresh = new Button { Text = "🔄 Tải lại", Size = new Size(110, 32), Location = new Point(10, 8) };
        ThemeManager.ApplySecondaryButton(btnRefresh);
        btnRefresh.Click += async (s, e) => await LoadPromotionsAsync();

        btnAdd = new Button { Text = "➕ Thêm Khuyến Mãi", Size = new Size(160, 32), Location = new Point(130, 8) };
        ThemeManager.ApplyPrimaryButton(btnAdd);
        btnAdd.Click += BtnAdd_Click;

        btnDelete = new Button { Text = "🗑️ Xóa Mã", Size = new Size(110, 32), Location = new Point(300, 8) };
        ThemeManager.ApplySecondaryButton(btnDelete);
        btnDelete.ForeColor = ThemeManager.Danger;
        btnDelete.Click += async (s, e) => await DeleteSelectedAsync();

        lblStatus = new Label
        {
            Text = "Sẵn sàng",
            Font = ThemeManager.BodyFont,
            ForeColor = ThemeManager.TextSecondary,
            Location = new Point(425, 14),
            AutoSize = true
        };

        pnlToolbar.Controls.Add(btnRefresh);
        pnlToolbar.Controls.Add(btnAdd);
        pnlToolbar.Controls.Add(btnDelete);
        pnlToolbar.Controls.Add(lblStatus);

        // Grid
        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10)
        };
        ThemeManager.ApplyCardPanel(pnlGridContainer);

        dgvPromotions = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvPromotions);
        dgvPromotions.Dock = DockStyle.Fill;

        dgvPromotions.Columns.Add("Code", "Mã Voucher");
        dgvPromotions.Columns.Add("Name", "Tên Chương Trình");
        dgvPromotions.Columns.Add("DiscountType", "Loại Giảm");
        dgvPromotions.Columns.Add("Discount", "Mức Giảm Giá");
        dgvPromotions.Columns.Add("Condition", "Đơn Hàng Tối Thiểu");
        dgvPromotions.Columns.Add("MaxDiscount", "Giảm Tối Đa");
        dgvPromotions.Columns.Add("TimeRange", "Thời Gian Hiệu Lực");
        dgvPromotions.Columns.Add("Status", "Trạng Thái");
        // Hidden columns for actions
        dgvPromotions.Columns.Add("RawId", "RawId");
        dgvPromotions.Columns["RawId"].Visible = false;

        pnlGridContainer.Controls.Add(dgvPromotions);

        this.Controls.Add(pnlGridContainer);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlHeader);
    }

    private async Task LoadPromotionsAsync()
    {
        lblStatus.Text = "Đang tải...";
        dgvPromotions.Rows.Clear();

        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/promotions?page=1&pageSize=100");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var data))
                {
                    var itemsProp = data.TryGetProperty("items", out var items) ? items : data;
                    if (itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsProp.EnumerateArray())
                        {
                            int id = item.GetProperty("promotionId").GetInt32();
                            string code = item.GetProperty("promotionCode").GetString() ?? "";
                            string name = item.GetProperty("promotionName").GetString() ?? "";
                            string discType = item.TryGetProperty("discountType", out var dt) ? (dt.GetString() ?? "Percentage") : "Percentage";
                            decimal discVal = item.GetProperty("discountValue").GetDecimal();
                            string discStr = discType.Equals("Percentage", StringComparison.OrdinalIgnoreCase)
                                ? $"{discVal:N0}%" : $"{discVal:N0} ₫";
                            decimal minOrder = item.TryGetProperty("minimumOrderAmount", out var mo) ? mo.GetDecimal() : 0m;
                            string maxStr = item.TryGetProperty("maximumDiscountAmount", out var mx) && mx.ValueKind != JsonValueKind.Null
                                ? $"{mx.GetDecimal():N0} ₫" : "Không giới hạn";
                            string discTypeLbl = discType.Equals("Percentage", StringComparison.OrdinalIgnoreCase)
                                ? "📊 % Phần Trăm" : "💰 Cố Định";

                            string startRaw = item.TryGetProperty("startDate", out var sd) ? sd.GetString() ?? "" : "";
                            string endRaw = item.TryGetProperty("endDate", out var ed) ? ed.GetString() ?? "" : "";
                            string timeRange = $"{ParseDateShort(startRaw)} → {ParseDateShort(endRaw)}";

                            bool isActive = item.TryGetProperty("isActive", out var ia) && ia.GetBoolean();
                            DateTime now = DateTime.UtcNow;
                            DateTime endDate = string.IsNullOrEmpty(endRaw) ? DateTime.MinValue : DateTime.Parse(endRaw);
                            string statusStr;
                            if (!isActive) statusStr = "🔴 Đã Tắt";
                            else if (endDate < now) statusStr = "⚪ Đã Hết Hạn";
                            else if ((endDate - now).TotalDays <= 3) statusStr = "⏳ Sắp Hết Hạn";
                            else statusStr = "🟢 Đang Diễn Ra";

                            dgvPromotions.Rows.Add(
                                code, name, discTypeLbl, discStr,
                                $"{minOrder:N0} ₫", maxStr, timeRange, statusStr, id
                            );
                        }
                        lblStatus.Text = $"Đã tải {dgvPromotions.Rows.Count} khuyến mãi.";
                        return;
                    }
                }
            }
            lblStatus.Text = "Không có dữ liệu hoặc API offline.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Lỗi: {ex.Message}";
        }
    }

    private static string ParseDateShort(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "-";
        if (DateTime.TryParse(raw, out var dt)) return dt.ToLocalTime().ToString("dd/MM/yyyy");
        return raw;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new AddPromotionForm(_apiBaseUrl, _httpClient);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadPromotionsAsync();
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (dgvPromotions.CurrentRow == null) return;
        var rawId = dgvPromotions.CurrentRow.Cells["RawId"].Value;
        if (rawId == null) return;

        int id = Convert.ToInt32(rawId);
        string code = dgvPromotions.CurrentRow.Cells["Code"].Value?.ToString() ?? $"ID-{id}";

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn XÓA mã khuyến mãi '{code}'?\nHành động này không thể hoàn tác.",
            "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            lblStatus.Text = $"Đang xóa {code}...";
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/v1/promotions/{id}");
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"✅ Đã xóa mã '{code}' thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadPromotionsAsync();
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Không thể xóa.\nChi tiết: {body}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Xóa thất bại.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

// ============================================================
// Form thêm mới khuyến mãi
// ============================================================
internal class AddPromotionForm : Form
{
    private readonly string _apiBaseUrl;
    private readonly HttpClient _httpClient;

    private TextBox txtCode = null!;
    private TextBox txtName = null!;
    private TextBox txtDesc = null!;
    private ComboBox cboType = null!;
    private NumericUpDown numValue = null!;
    private NumericUpDown numMinOrder = null!;
    private NumericUpDown numMaxDiscount = null!;
    private DateTimePicker dtpStart = null!;
    private DateTimePicker dtpEnd = null!;
    private CheckBox chkActive = null!;
    private Button btnSave = null!;
    private Button btnCancel_ = null!;

    public AddPromotionForm(string apiBaseUrl, HttpClient httpClient)
    {
        _apiBaseUrl = apiBaseUrl;
        _httpClient = httpClient;
        InitForm();
    }

    private void InitForm()
    {
        this.Text = "➕ Thêm Mã Khuyến Mãi";
        this.Size = new Size(480, 530);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = ThemeManager.Background;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        int y = 20;
        int lw = 160, cw = 270, lx = 15, cx = 180;

        Label Lbl(string text) => new Label { Text = text, Font = ThemeManager.BodyBold, ForeColor = ThemeManager.TextPrimary, Location = new Point(lx, y + 3), AutoSize = true };
        void NextRow(int h = 40) { y += h; }

        this.Controls.Add(Lbl("Mã Voucher:"));
        txtCode = new TextBox { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont };
        this.Controls.Add(txtCode); NextRow();

        this.Controls.Add(Lbl("Tên chương trình:"));
        txtName = new TextBox { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont };
        this.Controls.Add(txtName); NextRow();

        this.Controls.Add(Lbl("Mô tả:"));
        txtDesc = new TextBox { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont };
        this.Controls.Add(txtDesc); NextRow();

        this.Controls.Add(Lbl("Loại giảm giá:"));
        cboType = new ComboBox { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, DropDownStyle = ComboBoxStyle.DropDownList };
        cboType.Items.AddRange(new object[] { "Percentage", "FixedAmount" });
        cboType.SelectedIndex = 0;
        this.Controls.Add(cboType); NextRow();

        this.Controls.Add(Lbl("Giá trị giảm:"));
        numValue = new NumericUpDown { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, Maximum = 100000000, DecimalPlaces = 0, ThousandsSeparator = true };
        this.Controls.Add(numValue); NextRow();

        this.Controls.Add(Lbl("Đơn hàng tối thiểu:"));
        numMinOrder = new NumericUpDown { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, Maximum = 100000000, DecimalPlaces = 0, ThousandsSeparator = true };
        this.Controls.Add(numMinOrder); NextRow();

        this.Controls.Add(Lbl("Giảm tối đa (0=bỏ qua):"));
        numMaxDiscount = new NumericUpDown { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, Maximum = 100000000, DecimalPlaces = 0, ThousandsSeparator = true };
        this.Controls.Add(numMaxDiscount); NextRow();

        this.Controls.Add(Lbl("Ngày bắt đầu:"));
        dtpStart = new DateTimePicker { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, Value = DateTime.Today };
        this.Controls.Add(dtpStart); NextRow();

        this.Controls.Add(Lbl("Ngày kết thúc:"));
        dtpEnd = new DateTimePicker { Location = new Point(cx, y), Size = new Size(cw, 28), Font = ThemeManager.BodyFont, Value = DateTime.Today.AddMonths(1) };
        this.Controls.Add(dtpEnd); NextRow();

        this.Controls.Add(Lbl("Kích hoạt:"));
        chkActive = new CheckBox { Location = new Point(cx, y), Checked = true, Font = ThemeManager.BodyFont };
        this.Controls.Add(chkActive); NextRow(35);

        btnSave = new Button { Text = "💾 Lưu", Size = new Size(110, 36), Location = new Point(cx, y) };
        ThemeManager.ApplyPrimaryButton(btnSave);
        btnSave.Click += async (s, e) => await SaveAsync();
        this.Controls.Add(btnSave);

        btnCancel_ = new Button { Text = "Hủy", Size = new Size(80, 36), Location = new Point(cx + 120, y) };
        ThemeManager.ApplySecondaryButton(btnCancel_);
        btnCancel_.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        this.Controls.Add(btnCancel_);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Mã voucher và tên chương trình là bắt buộc.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var payload = new
        {
            promotionCode = txtCode.Text.Trim().ToUpper(),
            promotionName = txtName.Text.Trim(),
            description = txtDesc.Text.Trim(),
            discountType = cboType.SelectedItem?.ToString() ?? "Percentage",
            discountValue = numValue.Value,
            minimumOrderAmount = numMinOrder.Value,
            maximumDiscountAmount = numMaxDiscount.Value > 0 ? (decimal?)numMaxDiscount.Value : null,
            startDate = dtpStart.Value.ToUniversalTime(),
            endDate = dtpEnd.Value.ToUniversalTime(),
            isActive = chkActive.Checked
        };

        try
        {
            btnSave.Enabled = false;
            var json = JsonSerializer.Serialize(payload);
            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/promotions",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"✅ Đã tạo mã khuyến mãi '{txtCode.Text.ToUpper()}' thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Lỗi: {body}", "Không thể lưu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSave.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSave.Enabled = true;
        }
    }
}
