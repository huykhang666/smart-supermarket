using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Desktop.Services;

namespace Desktop.Views;

public class OrdersView : UserControl
{
    private DataGridView dgvOrders = null!;
    private Button btnRefresh = null!;
    private Button btnPos = null!;
    private Button btnViewDetail = null!;
    private Button btnExportPdf = null!;
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

        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MinimumSize = new Size(0, 52),
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        AppTheme.ApplyCardPanel(pnlToolbar);

        btnPos = new Button { Text = "🛒 Bán Hàng (POS)", Size = new Size(150, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplyPrimaryButton(btnPos);
        btnPos.Click += BtnPos_Click;

        btnRefresh = new Button { Text = "🔄 Tải lại", Size = new Size(95, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplySecondaryButton(btnRefresh);
        btnRefresh.Click += BtnRefresh_Click;

        btnViewDetail = new Button { Text = "🔍 Xem Chi Tiết", Size = new Size(130, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplySecondaryButton(btnViewDetail);
        btnViewDetail.Click += BtnViewDetail_Click;

        btnExportPdf = new Button { Text = "🖨️ In / Xuất PDF", Size = new Size(135, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplyPrimaryButton(btnExportPdf);
        btnExportPdf.Click += BtnExportPdf_Click;

        btnCancel = new Button { Text = "❌ Hủy Đơn", Size = new Size(110, 32), Margin = new Padding(0, 0, 8, 4) };
        AppTheme.ApplyDangerButton(btnCancel);
        btnCancel.Click += BtnCancel_Click;

        lblStatus = new Label
        {
            Text = "Sẵn sàng",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(8, 6, 0, 4),
            AutoSize = true
        };

        pnlToolbar.Controls.Add(btnPos);
        pnlToolbar.Controls.Add(btnRefresh);
        pnlToolbar.Controls.Add(btnViewDetail);
        pnlToolbar.Controls.Add(btnExportPdf);
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

        dgvOrders.Columns["OrderId"].FillWeight = 11;
        dgvOrders.Columns["Customer"].FillWeight = 14;
        dgvOrders.Columns["Employee"].FillWeight = 12;
        dgvOrders.Columns["TotalAmount"].FillWeight = 11;
        dgvOrders.Columns["DiscountAmount"].FillWeight = 10;
        dgvOrders.Columns["FinalAmount"].FillWeight = 12;
        dgvOrders.Columns["PaymentMethod"].FillWeight = 12;
        dgvOrders.Columns["OrderDate"].FillWeight = 14;
        dgvOrders.Columns["Status"].FillWeight = 10;

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

    private async void BtnExportPdf_Click(object? sender, EventArgs e)
    {
        if (dgvOrders.CurrentRow == null)
        {
            MessageBox.Show("Vui lòng chọn một đơn hàng trong danh sách để in hóa đơn PDF!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var row = dgvOrders.CurrentRow;
        var rawId = row.Cells["RawOrderId"].Value;
        if (rawId == null) return;
        int orderId = Convert.ToInt32(rawId);
        string code = row.Cells["OrderId"].Value?.ToString() ?? $"ORD-{orderId:D5}";

        lblStatus.Text = $"Đang xuất PDF cho {code}...";

        try
        {
            InvoicePrintModel model;

            // 1. Cố gắng lấy chi tiết đơn hàng từ Backend API
            HttpResponseMessage? response = null;
            try
            {
                response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/orders/{orderId}");
            }
            catch { }

            if (response != null && response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                var data = root.TryGetProperty("data", out var d) ? d : root;

                string custName = data.TryGetProperty("customerName", out var cn) && cn.ValueKind != JsonValueKind.Null ? (cn.GetString() ?? "Khách Lẻ") : "Khách Lẻ";
                string empName = data.TryGetProperty("employeeName", out var en) && en.ValueKind != JsonValueKind.Null ? (en.GetString() ?? "Thu ngân") : "Thu ngân";
                decimal totalAmount = data.GetProperty("totalAmount").GetDecimal();
                decimal discountAmount = data.TryGetProperty("discountAmount", out var da) ? da.GetDecimal() : 0m;
                decimal finalAmount = data.GetProperty("finalAmount").GetDecimal();
                DateTime orderDate = data.TryGetProperty("orderDate", out var od) ? DateTime.Parse(od.GetString() ?? DateTime.UtcNow.ToString()).ToLocalTime() : DateTime.Now;
                int payInt = data.TryGetProperty("paymentMethod", out var pm) ? pm.GetInt32() : 1;
                string payStr = payInt == 2 ? "VietQR" : payInt == 3 ? "Thẻ POS" : "Tiền mặt";

                var items = new List<InvoicePrintItem>();
                if (data.TryGetProperty("orderDetails", out var details) && details.ValueKind == JsonValueKind.Array)
                {
                    foreach (var itm in details.EnumerateArray())
                    {
                        string pName = itm.TryGetProperty("productName", out var pn) ? (pn.GetString() ?? "") : "Sản phẩm";
                        string barcode = itm.TryGetProperty("barcode", out var bc) ? (bc.GetString() ?? "") : "";
                        string unit = itm.TryGetProperty("unit", out var un) && un.ValueKind != JsonValueKind.Null ? (un.GetString() ?? "Cái") : "Cái";
                        int qty = itm.GetProperty("quantity").GetInt32();
                        decimal price = itm.GetProperty("unitPrice").GetDecimal();

                        items.Add(new InvoicePrintItem
                        {
                            ProductName = pName,
                            Barcode = barcode,
                            Unit = string.IsNullOrEmpty(unit) ? "Cái" : unit,
                            Quantity = qty,
                            UnitPrice = price
                        });
                    }
                }

                model = new InvoicePrintModel
                {
                    OrderCode = code,
                    OrderDate = orderDate,
                    CustomerName = custName,
                    CashierName = empName,
                    SubTotal = totalAmount,
                    DiscountTotal = discountAmount,
                    VatTotal = Math.Round(finalAmount - (finalAmount / 1.08m), 0),
                    GrandTotal = finalAmount,
                    PaymentMethod = payStr,
                    CashGiven = finalAmount,
                    ChangeDue = 0,
                    Items = items
                };
            }
            else
            {
                // 2. Fallback nếu dữ liệu lưu trong DataStore cục bộ
                var localOrder = DataStore.Orders.FirstOrDefault(o => o.OrderId == orderId || o.OrderCode == code);
                if (localOrder != null)
                {
                    model = new InvoicePrintModel
                    {
                        OrderCode = localOrder.OrderCode,
                        OrderDate = localOrder.OrderDate,
                        CustomerName = localOrder.CustomerName,
                        CashierName = "Thu ngân POS",
                        SubTotal = localOrder.SubTotal,
                        DiscountTotal = localOrder.DiscountTotal,
                        VatTotal = localOrder.VatTotal,
                        GrandTotal = localOrder.GrandTotal,
                        PaymentMethod = localOrder.PaymentMethod,
                        CashGiven = localOrder.GrandTotal,
                        ChangeDue = 0,
                        Items = localOrder.Items.Select(i => new InvoicePrintItem
                        {
                            ProductName = i.ProductName,
                            Barcode = i.Barcode,
                            Quantity = i.Quantity,
                            UnitPrice = i.Price
                        }).ToList()
                    };
                }
                else
                {
                    // Fallback từ các cột DataGridView
                    string cust = row.Cells["Customer"].Value?.ToString() ?? "Khách Lẻ";
                    string emp = row.Cells["Employee"].Value?.ToString() ?? "Thu ngân";
                    string finalStr = row.Cells["FinalAmount"].Value?.ToString()?.Replace("₫", "").Replace(",", "").Trim() ?? "0";
                    decimal finalAmt = decimal.TryParse(finalStr, out var fa) ? fa : 0m;
                    string payStr = row.Cells["PaymentMethod"].Value?.ToString() ?? "Tiền mặt";

                    model = new InvoicePrintModel
                    {
                        OrderCode = code,
                        OrderDate = DateTime.Now,
                        CustomerName = cust,
                        CashierName = emp,
                        SubTotal = finalAmt,
                        DiscountTotal = 0,
                        VatTotal = Math.Round(finalAmt - (finalAmt / 1.08m), 0),
                        GrandTotal = finalAmt,
                        PaymentMethod = payStr,
                        CashGiven = finalAmt,
                        ChangeDue = 0,
                        Items = new List<InvoicePrintItem>
                        {
                            new InvoicePrintItem { ProductName = "Mặt hàng theo đơn " + code, Quantity = 1, UnitPrice = finalAmt }
                        }
                    };
                }
            }

            string pdfPath = InvoicePdfService.PrintOrPreview(model, autoOpen: true);
            lblStatus.Text = $"Đã xuất hóa đơn {code}.";
            MessageBox.Show($"✅ Đã xuất và mở xem trước hóa đơn PDF thành công cho đơn {code}!\n\nFile đã lưu tại:\n{pdfPath}", "In Hóa Đơn PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Lỗi xuất PDF.";
            MessageBox.Show($"Lỗi khi xuất hóa đơn PDF: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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