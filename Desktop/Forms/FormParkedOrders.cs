using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Desktop.Models;

namespace Desktop.Forms;

public class FormParkedOrders : Form
{
    private readonly List<ParkedOrder> _parkedOrders;
    private DataGridView dgvOrders = null!;
    private DataGridView dgvDetails = null!;
    private Button btnResume = null!;
    private Button btnDelete = null!;
    private Button btnClose = null!;
    private Label lblSummary = null!;

    public ParkedOrder? SelectedOrderToResume { get; private set; }

    public FormParkedOrders(List<ParkedOrder> parkedOrders)
    {
        _parkedOrders = parkedOrders;
        InitializeComponent();
        LoadOrderList();
    }

    private void InitializeComponent()
    {
        this.Text = "Danh Sách Giỏ Hàng Đang Tạm Giữ (Parked Orders - F6)";
        this.Size = new Size(880, 520);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(244, 246, 248);
        this.Font = new Font("Segoe UI", 9.5f);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };
        var lblTitle = new Label
        {
            Text = "🛒 DANH SÁCH GIỎ HÀNG TẠM GIỮ (HOLD / PARK)",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(9, 109, 217),
            AutoSize = true,
            Location = new Point(15, 13)
        };
        lblSummary = new Label
        {
            Text = $"Đang lưu tạm: {_parkedOrders.Count} giỏ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(480, 15)
        };
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSummary);

        // Footer buttons
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };

        btnResume = new Button
        {
            Text = "⚡ MỞ LẠI ĐƠN NÀY (RESUME)",
            Size = new Size(220, 36),
            Location = new Point(15, 9),
            BackColor = Color.FromArgb(56, 158, 13),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnResume.Click += BtnResume_Click;

        btnDelete = new Button
        {
            Text = "🗑 Xóa Khỏi Danh Sách",
            Size = new Size(160, 36),
            Location = new Point(245, 9),
            BackColor = Color.FromArgb(255, 77, 79),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnDelete.Click += BtnDelete_Click;

        btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(100, 36),
            Location = new Point(745, 9),
            BackColor = Color.LightGray,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        pnlFooter.Controls.Add(btnResume);
        pnlFooter.Controls.Add(btnDelete);
        pnlFooter.Controls.Add(btnClose);

        // Body: Split container
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 460,
            Padding = new Padding(15)
        };

        // Left: Grid đơn hàng
        var grpOrders = new GroupBox
        {
            Text = "Các Giỏ Hàng Đang Treo",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        dgvOrders = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9f)
        };
        dgvOrders.Columns.Add("ParkId", "Mã Tạm");
        dgvOrders.Columns.Add("ParkedAt", "Thời Gian");
        dgvOrders.Columns.Add("Customer", "Khách Hàng");
        dgvOrders.Columns.Add("ItemCount", "Số Món");
        dgvOrders.Columns.Add("TotalAmount", "Tổng Tiền");
        dgvOrders.SelectionChanged += DgvOrders_SelectionChanged;

        grpOrders.Controls.Add(dgvOrders);
        split.Panel1.Controls.Add(grpOrders);

        // Right: Grid chi tiết món
        var grpDetails = new GroupBox
        {
            Text = "Chi Tiết Sản Phẩm Trong Giỏ",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        dgvDetails = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9f)
        };
        dgvDetails.Columns.Add("ProductName", "Tên Sản Phẩm");
        dgvDetails.Columns.Add("Quantity", "SL");
        dgvDetails.Columns.Add("Price", "Đơn Giá");
        dgvDetails.Columns.Add("SubTotal", "Thành Tiền");

        grpDetails.Controls.Add(dgvDetails);
        split.Panel2.Controls.Add(grpDetails);

        this.Controls.Add(split);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlFooter);
    }

    private void LoadOrderList()
    {
        dgvOrders.Rows.Clear();
        foreach (var order in _parkedOrders)
        {
            string cust = !string.IsNullOrEmpty(order.CustomerName) ? order.CustomerName : (!string.IsNullOrEmpty(order.CustomerPhone) ? order.CustomerPhone : "Khách Lẻ");
            dgvOrders.Rows.Add(
                order.ParkId,
                order.ParkedAt.ToString("HH:mm:ss dd/MM"),
                cust,
                order.Items.Sum(i => i.Quantity),
                $"{order.FinalAmount:N0} đ"
            );
        }

        lblSummary.Text = $"Đang lưu tạm: {_parkedOrders.Count} giỏ";
        btnResume.Enabled = _parkedOrders.Count > 0;
        btnDelete.Enabled = _parkedOrders.Count > 0;

        if (dgvOrders.Rows.Count > 0)
        {
            dgvOrders.Rows[0].Selected = true;
            ShowOrderDetails(0);
        }
        else
        {
            dgvDetails.Rows.Clear();
        }
    }

    private void DgvOrders_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvOrders.CurrentRow != null && dgvOrders.CurrentRow.Index >= 0)
        {
            ShowOrderDetails(dgvOrders.CurrentRow.Index);
        }
    }

    private void ShowOrderDetails(int index)
    {
        if (index < 0 || index >= _parkedOrders.Count) return;
        var order = _parkedOrders[index];

        dgvDetails.Rows.Clear();
        foreach (var item in order.Items)
        {
            dgvDetails.Rows.Add(item.Name, item.Quantity, $"{item.Price:N0} đ", $"{item.SubTotal:N0} đ");
        }
    }

    private void BtnResume_Click(object? sender, EventArgs e)
    {
        if (dgvOrders.CurrentRow == null || dgvOrders.CurrentRow.Index < 0) return;
        int idx = dgvOrders.CurrentRow.Index;
        if (idx >= 0 && idx < _parkedOrders.Count)
        {
            SelectedOrderToResume = _parkedOrders[idx];
            _parkedOrders.RemoveAt(idx);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvOrders.CurrentRow == null || dgvOrders.CurrentRow.Index < 0) return;
        int idx = dgvOrders.CurrentRow.Index;
        if (idx >= 0 && idx < _parkedOrders.Count)
        {
            var res = MessageBox.Show($"Bạn có chắc muốn xóa giỏ hàng tạm [{_parkedOrders[idx].ParkId}] không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                _parkedOrders.RemoveAt(idx);
                LoadOrderList();
            }
        }
    }
}
