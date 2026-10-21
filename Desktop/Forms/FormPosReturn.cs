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

namespace Desktop.Forms;

public class FormPosReturn : Form
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly int _employeeId;

    private TextBox txtOrderCode = null!;
    private Button btnSearchOrder = null!;
    private Label lblOrderInfo = null!;
    private DataGridView dgvItems = null!;
    private Label lblTotalRefund = null!;
    private TextBox txtReason = null!;
    private Button btnConfirmRefund = null!;
    private Button btnClose = null!;

    private int _currentOrderId = 0;
    private decimal _totalRefundAmount = 0m;

    private class ReturnRowItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int OriginalQty { get; set; }
        public int ReturnQty { get; set; }
        public decimal RefundSubTotal => UnitPrice * ReturnQty;
    }

    private readonly List<ReturnRowItem> _returnItems = new();

    public FormPosReturn(HttpClient httpClient, string apiBaseUrl, int employeeId = 1)
    {
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl;
        _employeeId = employeeId;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Đổi / Trả Hàng Tại Quầy POS (Return & Refund - F7)";
        this.Size = new Size(860, 560);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(244, 246, 248);
        this.Font = new Font("Segoe UI", 9.5f);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.White,
            Padding = new Padding(15, 12, 15, 10)
        };

        var lblTitle = new Label
        {
            Text = "🔄 ĐỔI / TRẢ HÀNG & HOÀN TIỀN (F7)",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(217, 83, 79),
            AutoSize = true,
            Location = new Point(15, 8)
        };

        var lblScan = new Label
        {
            Text = "Mã hóa đơn (Quét barcode trên bill cũ hoặc gõ mã / ID):",
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Location = new Point(15, 33)
        };

        txtOrderCode = new TextBox
        {
            Location = new Point(15, 52),
            Size = new Size(340, 26),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            PlaceholderText = "Ví dụ: ORD-00012 hoặc 12..."
        };
        txtOrderCode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnSearchOrder_Click(s, e); };

        btnSearchOrder = new Button
        {
            Text = "🔍 Tìm Bill",
            Location = new Point(365, 51),
            Size = new Size(110, 28),
            BackColor = Color.FromArgb(9, 109, 217),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSearchOrder.Click += BtnSearchOrder_Click;

        lblOrderInfo = new Label
        {
            Text = "Chưa nạp hóa đơn",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(490, 56)
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblScan);
        pnlHeader.Controls.Add(txtOrderCode);
        pnlHeader.Controls.Add(btnSearchOrder);
        pnlHeader.Controls.Add(lblOrderInfo);

        // Footer
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 110,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };

        var lblReason = new Label { Text = "Lý do đổi trả:", Location = new Point(15, 12), AutoSize = true };
        txtReason = new TextBox { Location = new Point(105, 10), Size = new Size(370, 25), Text = "Khách đổi trả hàng theo hóa đơn" };

        lblTotalRefund = new Label
        {
            Text = "TỔNG TIỀN HOÀN: 0 ₫",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(217, 83, 79),
            Location = new Point(500, 10),
            AutoSize = true
        };

        btnConfirmRefund = new Button
        {
            Text = "✔ XÁC NHẬN HOÀN TIỀN & NHẬP KHO",
            Size = new Size(280, 42),
            Location = new Point(15, 48),
            BackColor = Color.FromArgb(56, 158, 13),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Enabled = false
        };
        btnConfirmRefund.Click += BtnConfirmRefund_Click;

        btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(110, 42),
            Location = new Point(310, 48),
            BackColor = Color.LightGray,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClose.Click += (s, e) => this.Close();

        pnlFooter.Controls.Add(lblReason);
        pnlFooter.Controls.Add(txtReason);
        pnlFooter.Controls.Add(lblTotalRefund);
        pnlFooter.Controls.Add(btnConfirmRefund);
        pnlFooter.Controls.Add(btnClose);

        // Grid chi tiết món
        var pnlCenter = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        dgvItems = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 32 }
        };

        dgvItems.Columns.Add("ProductId", "ID");
        dgvItems.Columns["ProductId"].Visible = false;
        dgvItems.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvItems.Columns["ProductName"].ReadOnly = true;
        dgvItems.Columns.Add("UnitPrice", "Đơn Giá Mua");
        dgvItems.Columns["UnitPrice"].ReadOnly = true;
        dgvItems.Columns.Add("OriginalQty", "SL Đã Mua");
        dgvItems.Columns["OriginalQty"].ReadOnly = true;

        var colReturn = new DataGridViewTextBoxColumn
        {
            Name = "ReturnQty",
            HeaderText = "SL Trả (Nhập số)",
            DefaultCellStyle = { ForeColor = Color.Red, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
        };
        dgvItems.Columns.Add(colReturn);

        dgvItems.Columns.Add("RefundSubTotal", "Tiền Hoàn");
        dgvItems.Columns["RefundSubTotal"].ReadOnly = true;

        dgvItems.CellValueChanged += DgvItems_CellValueChanged;
        dgvItems.CellEndEdit += (s, e) => RecalculateRefundTotal();

        pnlCenter.Controls.Add(dgvItems);

        this.Controls.Add(pnlCenter);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlFooter);
    }

    private async void BtnSearchOrder_Click(object? sender, EventArgs e)
    {
        string raw = txtOrderCode.Text.Trim();
        if (string.IsNullOrEmpty(raw)) return;

        // Parse orderId từ chuỗi nhập (Ví dụ: ORD-00012 -> 12 hoặc 12)
        int orderId = 0;
        string clean = raw.Replace("ORD-", "", StringComparison.OrdinalIgnoreCase).Replace("HD-", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!int.TryParse(clean, out orderId) || orderId <= 0)
        {
            MessageBox.Show("Mã hóa đơn không hợp lệ! Vui lòng nhập đúng định dạng (VD: ORD-00012 hoặc 12).", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnSearchOrder.Enabled = false;
            btnSearchOrder.Text = "Đang tìm...";

            var res = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/orders/{orderId}");
            if (!res.IsSuccessStatusCode)
            {
                MessageBox.Show($"Không tìm thấy đơn hàng #{orderId} trên hệ thống.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : root;

            _currentOrderId = data.GetProperty("orderId").GetInt32();
            string custName = data.TryGetProperty("customerName", out var cn) && cn.ValueKind != JsonValueKind.Null ? cn.GetString() ?? "Khách Lẻ" : "Khách Lẻ";
            decimal finalAmount = data.GetProperty("finalAmount").GetDecimal();
            string statusStr = data.TryGetProperty("status", out var st) ? (st.ValueKind == JsonValueKind.String ? st.GetString() ?? "" : st.GetInt32().ToString()) : "";

            lblOrderInfo.Text = $"Bill: ORD-{_currentOrderId:D5} | Khách: {custName} | Tổng TT: {finalAmount:N0} đ | Trạng thái: {statusStr}";
            lblOrderInfo.ForeColor = Color.DarkGreen;

            _returnItems.Clear();
            dgvItems.Rows.Clear();

            if (data.TryGetProperty("orderDetails", out var details) && details.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in details.EnumerateArray())
                {
                    int pId = item.GetProperty("productId").GetInt32();
                    string pName = item.TryGetProperty("productName", out var pn) ? (pn.GetString() ?? "Sản phẩm") : "Sản phẩm";
                    decimal price = item.GetProperty("unitPrice").GetDecimal();
                    int qty = item.GetProperty("quantity").GetInt32();

                    if (qty > 0)
                    {
                        var row = new ReturnRowItem
                        {
                            ProductId = pId,
                            ProductName = pName,
                            UnitPrice = price,
                            OriginalQty = qty,
                            ReturnQty = 0
                        };
                        _returnItems.Add(row);
                        dgvItems.Rows.Add(pId, pName, $"{price:N0} ₫", qty, 0, "0 ₫");
                    }
                }
            }

            RecalculateRefundTotal();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi truy vấn đơn hàng: {ex.Message}", "Lỗi");
        }
        finally
        {
            btnSearchOrder.Enabled = true;
            btnSearchOrder.Text = "🔍 Tìm Bill";
        }
    }

    private void DgvItems_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != dgvItems.Columns["ReturnQty"].Index) return;

        var row = dgvItems.Rows[e.RowIndex];
        int origQty = Convert.ToInt32(row.Cells["OriginalQty"].Value);
        string valStr = Convert.ToString(row.Cells["ReturnQty"].Value) ?? "0";

        if (!int.TryParse(valStr, out int returnQty) || returnQty < 0)
        {
            returnQty = 0;
            row.Cells["ReturnQty"].Value = 0;
        }

        if (returnQty > origQty)
        {
            MessageBox.Show($"Số lượng trả ({returnQty}) không được vượt quá số lượng đã mua ({origQty})!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            returnQty = origQty;
            row.Cells["ReturnQty"].Value = origQty;
        }

        if (e.RowIndex < _returnItems.Count)
        {
            _returnItems[e.RowIndex].ReturnQty = returnQty;
            decimal sub = _returnItems[e.RowIndex].RefundSubTotal;
            row.Cells["RefundSubTotal"].Value = $"{sub:N0} ₫";
        }
    }

    private void RecalculateRefundTotal()
    {
        _totalRefundAmount = _returnItems.Sum(x => x.RefundSubTotal);
        lblTotalRefund.Text = $"TỔNG TIỀN HOÀN: {_totalRefundAmount:N0} ₫";
        btnConfirmRefund.Enabled = _currentOrderId > 0 && _totalRefundAmount > 0;
    }

    private async void BtnConfirmRefund_Click(object? sender, EventArgs e)
    {
        var itemsToRefund = _returnItems.Where(x => x.ReturnQty > 0).ToList();
        if (!itemsToRefund.Any())
        {
            MessageBox.Show("Vui lòng nhập số lượng sản phẩm cần trả trước khi xác nhận.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"XÁC NHẬN TRẢ HÀNG & HOÀN TIỀN:\n\n" +
            $"• Mã hóa đơn: ORD-{_currentOrderId:D5}\n" +
            $"• Tổng số món trả: {itemsToRefund.Sum(x => x.ReturnQty)}\n" +
            $"• Số tiền cần hoàn trả cho khách: {_totalRefundAmount:N0} VNĐ\n\n" +
            $"Hệ thống sẽ hoàn trả số tiền này và tự động CỘNG LẠI TỒN KHO cho từng sản phẩm.\nBạn có chắc chắn tiếp tục?",
            "Xác nhận hoàn tiền", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            btnConfirmRefund.Enabled = false;
            btnConfirmRefund.Text = "Đang xử lý...";

            var payload = new
            {
                orderId = _currentOrderId,
                employeeId = _employeeId,
                reason = txtReason.Text.Trim(),
                items = itemsToRefund.Select(x => new
                {
                    productId = x.ProductId,
                    quantityToRefund = x.ReturnQty,
                    reason = txtReason.Text.Trim()
                }).ToList()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/orders/{_currentOrderId}/refund", content);
            if (res.IsSuccessStatusCode)
            {
                // In phiếu hoàn trả K80
                PrintRefundThermalSlip(itemsToRefund);

                MessageBox.Show(
                    $"✅ HOÀN TẤT ĐỔI TRẢ HÀNG THÀNH CÔNG!\n\n" +
                    $"• Số tiền đã hoàn: {_totalRefundAmount:N0} VNĐ\n" +
                    $"• Đã hoàn trả tồn kho và cập nhật nhật ký kho (StockHistory).\n" +
                    $"• Đã xuất biên lai hoàn tiền.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Lỗi từ máy chủ:\n{err}", "Không thể hoàn trả", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi");
        }
        finally
        {
            btnConfirmRefund.Enabled = true;
            btnConfirmRefund.Text = "✔ XÁC NHẬN HOÀN TIỀN & NHẬP KHO";
        }
    }

    private void PrintRefundThermalSlip(List<ReturnRowItem> refundedItems)
    {
        try
        {
            var printModel = new InvoicePrintModel
            {
                OrderCode = $"REF-{_currentOrderId:D5}",
                OrderDate = DateTime.Now,
                CustomerName = "Phiếu Trả Hàng (Return Slip)",
                CashierName = $"Thu ngân #{_employeeId}",
                SubTotal = _totalRefundAmount,
                GrandTotal = _totalRefundAmount,
                PaymentMethod = "Hoàn Tiền Mặt",
                CashGiven = _totalRefundAmount,
                ChangeDue = 0,
                Items = refundedItems.Select(x => new InvoicePrintItem
                {
                    ProductName = $"[TRẢ] {x.ProductName}",
                    Quantity = x.ReturnQty,
                    UnitPrice = x.UnitPrice
                }).ToList()
            };

            ThermalBillPrinterService.PrintOrPreview(printModel, directPrint: false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FormPosReturn] Print slip error: {ex.Message}");
        }
    }
}
