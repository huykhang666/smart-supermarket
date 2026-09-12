using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class OrdersView : UserControl
{
    private DataGridView dgvOrders = null!;
    private Button btnRefresh = null!;
    private Button btnCancel = null!;
    private Button btnViewDetail = null!;
    private Label lblStatus = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = "http://localhost:5137";

    public OrdersView()
    {
        InitializeComponent();
        _ = LoadOrdersAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(20, 15, 20, 15),
            Margin = new Padding(0, 0, 0, 15)
        };
        ThemeManager.ApplyRoundedCardPanel(pnlHeader, 10);

        var lblTitle = new Label
        {
            Text = "📦 QUẢN LÝ ĐƠN HÀNG & TIẾN TRÌNH (ORDERS & TIMELINE)",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.NavyBrand,
            Location = new Point(20, 18),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        // Toolbar Panel
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10, 8, 10, 8),
        };
        ThemeManager.ApplyCardPanel(pnlToolbar);

        btnRefresh = new Button { Text = "🔄 Tải lại", Size = new Size(110, 32), Location = new Point(10, 8) };
        ThemeManager.ApplySecondaryButton(btnRefresh);
        btnRefresh.Click += async (s, e) => await LoadOrdersAsync();

        btnViewDetail = new Button { Text = "🔍 Xem Chi Tiết", Size = new Size(135, 32), Location = new Point(130, 8) };
        ThemeManager.ApplySecondaryButton(btnViewDetail);
        btnViewDetail.Click += BtnViewDetail_Click;

        btnCancel = new Button { Text = "❌ Hủy Đơn Hàng", Size = new Size(140, 32), Location = new Point(275, 8) };
        ThemeManager.ApplySecondaryButton(btnCancel);
        btnCancel.ForeColor = ThemeManager.Danger;
        btnCancel.Click += async (s, e) => await CancelSelectedOrderAsync();

        lblStatus = new Label
        {
            Text = "Sẵn sàng",
            Font = ThemeManager.BodyFont,
            ForeColor = ThemeManager.TextSecondary,
            Location = new Point(430, 14),
            AutoSize = true
        };

        pnlToolbar.Controls.Add(btnRefresh);
        pnlToolbar.Controls.Add(btnViewDetail);
        pnlToolbar.Controls.Add(btnCancel);
        pnlToolbar.Controls.Add(lblStatus);

        // Grid Container
        var pnlGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(10)
        };
        ThemeManager.ApplyCardPanel(pnlGrid);

        dgvOrders = new DataGridView();
        ThemeManager.ApplyGridStyle(dgvOrders);
        dgvOrders.Dock = DockStyle.Fill;

        dgvOrders.Columns.Add("OrderId", "Mã Đơn");
        dgvOrders.Columns.Add("Customer", "Khách Hàng");
        dgvOrders.Columns.Add("Employee", "Thu Ngân / NV");
        dgvOrders.Columns.Add("TotalAmount", "Tạm Tính");
        dgvOrders.Columns.Add("DiscountAmount", "Giảm Giá");
        dgvOrders.Columns.Add("FinalAmount", "Thành Tiền");
        dgvOrders.Columns.Add("PaymentMethod", "Thanh Toán");
        dgvOrders.Columns.Add("OrderDate", "Ngày Đặt");
        dgvOrders.Columns.Add("Status", "Trạng Thái");
        // Hidden column to hold the real orderId for actions
        dgvOrders.Columns.Add("RawOrderId", "RawOrderId");
        dgvOrders.Columns["RawOrderId"].Visible = false;

        pnlGrid.Controls.Add(dgvOrders);

        // Layout: top → header, then toolbar, then grid fills remaining
        this.Controls.Add(pnlGrid);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlHeader);
    }

    private async Task LoadOrdersAsync()
    {
        lblStatus.Text = "Đang tải...";
        dgvOrders.Rows.Clear();

        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/orders?page=1&pageSize=50");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Response: { data: { items: [...], totalCount: x } }
                if (root.TryGetProperty("data", out var data))
                {
                    var itemsProp = data.TryGetProperty("items", out var items) ? items : data;
                    if (itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsProp.EnumerateArray())
                        {
                            int id = item.GetProperty("orderId").GetInt32();
                            string cust = item.TryGetProperty("customerName", out var c) && c.ValueKind != JsonValueKind.Null
                                ? (c.GetString() ?? "Khách Lẻ") : "Khách Lẻ";
                            string emp = item.TryGetProperty("employeeName", out var en) && en.ValueKind != JsonValueKind.Null
                                ? (en.GetString() ?? "-") : "-";
                            decimal total = item.GetProperty("totalAmount").GetDecimal();
                            decimal discount = item.TryGetProperty("discountAmount", out var d2) ? d2.GetDecimal() : 0m;
                            decimal final_ = item.GetProperty("finalAmount").GetDecimal();
                            int payInt = item.TryGetProperty("paymentMethod", out var pm) ? pm.GetInt32() : 1;
                            string payStr = payInt == 2 ? "📱 VietQR" : payInt == 3 ? "💳 Thẻ POS" : "💵 Tiền Mặt";
                            string orderDate = item.TryGetProperty("orderDate", out var od)
                                ? DateTime.Parse(od.GetString() ?? DateTime.UtcNow.ToString()).ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                                : "-";
                            int statusInt = item.TryGetProperty("status", out var st) ? st.GetInt32() : 1;
                            string statusStr = statusInt == 2 ? "❌ Đã Hủy" : "✅ Hoàn Thành";

                            dgvOrders.Rows.Add(
                                $"ORD-{id:D5}", cust, emp,
                                $"{total:N0} ₫", discount > 0 ? $"-{discount:N0} ₫" : "-",
                                $"{final_:N0} ₫", payStr, orderDate, statusStr, id
                            );
                        }
                        lblStatus.Text = $"Đã tải {dgvOrders.Rows.Count} đơn hàng.";
                        return;
                    }
                }
            }
            lblStatus.Text = "Không có dữ liệu hoặc API offline.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Lỗi kết nối: {ex.Message}";
        }
    }

    private void BtnViewDetail_Click(object? sender, EventArgs e)
    {
        if (dgvOrders.CurrentRow == null) return;
        var row = dgvOrders.CurrentRow;
        string orderId = row.Cells["OrderId"].Value?.ToString() ?? "";
        string customer = row.Cells["Customer"].Value?.ToString() ?? "";
        string total = row.Cells["FinalAmount"].Value?.ToString() ?? "";
        string payment = row.Cells["PaymentMethod"].Value?.ToString() ?? "";
        string date = row.Cells["OrderDate"].Value?.ToString() ?? "";
        string status = row.Cells["Status"].Value?.ToString() ?? "";

        MessageBox.Show(
            $"ℹ️ Chi tiết đơn hàng:\n\n" +
            $"• Mã đơn: {orderId}\n" +
            $"• Khách hàng: {customer}\n" +
            $"• Tổng tiền thực thanh toán: {total}\n" +
            $"• Hình thức TT: {payment}\n" +
            $"• Ngày đặt: {date}\n" +
            $"• Trạng thái: {status}",
            "Chi tiết đơn hàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task CancelSelectedOrderAsync()
    {
        if (dgvOrders.CurrentRow == null) return;
        var rawId = dgvOrders.CurrentRow.Cells["RawOrderId"].Value;
        if (rawId == null) return;

        int orderId = Convert.ToInt32(rawId);
        string maDon = dgvOrders.CurrentRow.Cells["OrderId"].Value?.ToString() ?? $"ORD-{orderId:D5}";

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn HỦY đơn hàng {maDon}?\nHành động này không thể hoàn tác.",
            "Xác nhận hủy đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            lblStatus.Text = $"Đang hủy {maDon}...";
            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/v1/orders/{orderId}/cancel",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"✅ Đơn hàng {maDon} đã được hủy thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadOrdersAsync();
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Không thể hủy đơn hàng.\nChi tiết: {body}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Hủy thất bại.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Lỗi kết nối.";
        }
    }
}
