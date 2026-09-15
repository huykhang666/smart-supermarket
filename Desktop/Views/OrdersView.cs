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
    private Button btnPos = null!;
    private Button btnViewDetail = null!;
    private Button btnCancel = null!;
    private Label lblStatus = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public OrdersView()
    {
        InitializeComponent();
        _ = LoadOrdersAsync();
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
            Text = "Quản Lý Đơn Hàng & Lịch Sử POS",
            Font = AppTheme.FontH1,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 4),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlToolbar);

        // Nút Mở POS Bán Hàng
        btnPos = new Button { Text = "🛒 Bán Hàng (POS)", Size = new Size(150, 32), Location = new Point(12, 10) };
        AppTheme.ApplyPrimaryButton(btnPos);
        btnPos.Click += BtnPos_Click;

        // Nút Tải Lại
        btnRefresh = new Button { Text = "🔄 Tải lại", Size = new Size(95, 32), Location = new Point(170, 10) };
        AppTheme.ApplySecondaryButton(btnRefresh);
        btnRefresh.Click += BtnRefresh_Click;

        // Nút Xem Chi Tiết
        btnViewDetail = new Button { Text = "🔍 Xem Chi Tiết", Size = new Size(130, 32), Location = new Point(275, 10) };
        AppTheme.ApplySecondaryButton(btnViewDetail);
        btnViewDetail.Click += BtnViewDetail_Click;

        // Nút Hủy Đơn Hàng
        btnCancel = new Button { Text = "❌ Hủy Đơn", Size = new Size(110, 32), Location = new Point(415, 10) };
        AppTheme.ApplyDangerButton(btnCancel);
        btnCancel.Click += BtnCancel_Click;

        // Nhãn Trạng Thái
        lblStatus = new Label
        {
            Text = "Sẵn sàng",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(540, 16),
            AutoSize = true
        };

        pnlToolbar.Controls.Add(btnPos);
        pnlToolbar.Controls.Add(btnRefresh);
        pnlToolbar.Controls.Add(btnViewDetail);
        pnlToolbar.Controls.Add(btnCancel);
        pnlToolbar.Controls.Add(lblStatus);

        var pnlGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(1)
        };
        AppTheme.ApplyCardPanel(pnlGrid);

        dgvOrders = new DataGridView();
        AppTheme.ApplyGridStyle(dgvOrders);
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
        dgvOrders.Columns.Add("RawOrderId", "RawOrderId");
        dgvOrders.Columns["RawOrderId"].Visible = false;

        pnlGrid.Controls.Add(dgvOrders);

        this.Controls.Add(pnlGrid);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlHeader);
    }

    private void BtnPos_Click(object? sender, EventArgs e)
    {
        using var posForm = new PosOrderForm(_httpClient, _apiBaseUrl);
        if (posForm.ShowDialog() == DialogResult.OK)
        {
            _ = LoadOrdersAsync();
        }
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e)
    {
        await LoadOrdersAsync();
    }

    private async void BtnCancel_Click(object? sender, EventArgs e)
    {
        await CancelSelectedOrderAsync();
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