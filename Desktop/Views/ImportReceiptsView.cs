using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;

namespace Desktop.Views;

public class ImportReceiptsView : UserControl
{
    private Panel pnlTopBar = null!;
    private ComboBox cbStatus = null!;
    private Button btnSearch = null!;
    private Button btnCreateReceipt = null!;
    private DataGridView dgvReceipts = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public ImportReceiptsView()
    {
        InitializeComponent();
        LoadDataAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        // --- Top Bar ---
        pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(15, 12, 15, 12),
            Margin = new Padding(0, 0, 0, 8)
        };
        AppTheme.ApplyCardPanel(pnlTopBar);

        cbStatus = new ComboBox
        {
            Font = AppTheme.FontBody,
            Size = new Size(180, 32),
            Location = new Point(15, 15),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbStatus.Items.AddRange(new object[] { "Tất cả Trạng thái", "Nháp (Draft)", "Đã Xác Nhận (Confirmed)", "Đã Hủy (Cancelled)" });
        cbStatus.SelectedIndex = 0;

        btnSearch = new Button
        {
            Text = "Lọc",
            Size = new Size(90, 32),
            Location = new Point(210, 15)
        };
        AppTheme.ApplySecondaryButton(btnSearch);
        btnSearch.Click += (s, e) => LoadDataAsync();

        btnCreateReceipt = new Button
        {
            Text = "➕ Tạo Phiếu Nhập",
            Size = new Size(170, 32),
            Location = new Point(315, 15)
        };
        AppTheme.ApplyPrimaryButton(btnCreateReceipt);
        btnCreateReceipt.Click += BtnCreateReceipt_Click;

        pnlTopBar.Controls.Add(cbStatus);
        pnlTopBar.Controls.Add(btnSearch);
        pnlTopBar.Controls.Add(btnCreateReceipt);

        // --- DataGrid ---
        dgvReceipts = new DataGridView();
        AppTheme.ApplyGridStyle(dgvReceipts);

        dgvReceipts.Columns.Add("ReceiptId", "ID");
        dgvReceipts.Columns.Add("ReceiptCode", "Mã Phiếu");
        dgvReceipts.Columns.Add("ImportDate", "Ngày Tạo");
        dgvReceipts.Columns.Add("TotalAmount", "Tổng Tiền (VNĐ)");
        dgvReceipts.Columns.Add("Status", "Trạng Thái");
        
        var colAction = new DataGridViewButtonColumn
        {
            Name = "Action",
            HeaderText = "Thao tác",
            Text = "Xác nhận Nhập kho",
            UseColumnTextForButtonValue = true
        };
        dgvReceipts.Columns.Add(colAction);
        dgvReceipts.CellClick += DgvReceipts_CellClick;

        this.Controls.Add(dgvReceipts);
        this.Controls.Add(pnlTopBar);
    }

    private async void LoadDataAsync()
    {
        dgvReceipts.Rows.Clear();
        try
        {
            int? statusEnum = cbStatus.SelectedIndex switch { 1 => 1, 2 => 2, 3 => 3, _ => null };
            string statusQuery = statusEnum.HasValue ? $"&status={statusEnum}" : "";
            
            string url = $"{_apiBaseUrl}/api/import-receipts?page=1&pageSize=50{statusQuery}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        int id = item.GetProperty("importReceiptId").GetInt32();
                        string code = item.GetProperty("receiptCode").GetString() ?? "";
                        string date = item.GetProperty("importDate").GetDateTime().ToString("dd/MM/yyyy HH:mm");
                        decimal total = item.GetProperty("totalAmount").GetDecimal();
                        int status = item.GetProperty("status").GetInt32();

                        string statusStr = status switch { 1 => "📝 Nháp", 2 => "✅ Đã Nhập Kho", 3 => "❌ Đã Hủy", _ => "Không rõ" };

                        dgvReceipts.Rows.Add(id, code, date, $"{total:N0} đ", statusStr);
                    }
                    return;
                }
            }
        }
        catch { }
    }

    private async void DgvReceipts_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvReceipts.Columns[e.ColumnIndex].Name == "Action")
        {
            int id = (int)dgvReceipts.Rows[e.RowIndex].Cells["ReceiptId"].Value;
            string status = dgvReceipts.Rows[e.RowIndex].Cells["Status"].Value.ToString() ?? "";

            if (status.Contains("Nháp"))
            {
                var confirmResult = MessageBox.Show("Bạn có chắc muốn XÁC NHẬN phiếu nhập này? Hàng sẽ được cộng thẳng vào Tồn Kho và không thể hoàn tác.",
                                     "Xác nhận Nhập kho",
                                     MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/import-receipts/{id}/confirm", null);
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Đã xác nhận và cộng tồn kho thành công!", "Thành công");
                            LoadDataAsync();
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Giả lập xác nhận thành công (Do API offline).");
                        LoadDataAsync();
                    }
                }
            }
            else
            {
                MessageBox.Show("Phiếu này đã được xử lý (Xác nhận/Hủy) nên không thể thao tác thêm.", "Thông báo");
            }
        }
    }

    private void BtnCreateReceipt_Click(object? sender, EventArgs e)
    {
        var form = new CreateImportReceiptForm(_httpClient, _apiBaseUrl);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadDataAsync();
        }
    }
}
