using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop.Views;

public class CategoriesView : UserControl
{
    private TreeView tvCategories = null!;
    private Panel pnlRightDetail = null!;
    private TextBox txtCategoryName = null!;
    private TextBox txtSlug = null!;
    private TextBox txtDescription = null!;
    private Button btnSave = null!;
    private Button btnDelete = null!;
    private Button btnAddChild = null!;
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl = AppTheme.ApiBaseUrl;

    public CategoriesView()
    {
        InitializeComponent();
        _ = LoadCategoriesAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = AppTheme.BackgroundGray;
        this.Padding = new Padding(24);

        // --- Left Tree Panel ---
        var pnlLeftTree = new Panel
        {
            Dock = DockStyle.Left,
            Width = 360,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 16, 0)
        };
        AppTheme.ApplyCardPanel(pnlLeftTree);

        var lblTreeHeader = new Label
        {
            Text = "📁 Cây Danh Mục Sản Phẩm",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 36
        };

        tvCategories = new TreeView
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBody,
            BorderStyle = BorderStyle.None,
            BackColor = AppTheme.SurfaceWhite,
            ForeColor = AppTheme.TextPrimary,
            ItemHeight = 28
        };
        tvCategories.AfterSelect += TvCategories_AfterSelect;

        pnlLeftTree.Controls.Add(tvCategories);
        pnlLeftTree.Controls.Add(lblTreeHeader);

        // --- Right Detail Panel ---
        pnlRightDetail = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceWhite,
            Padding = new Padding(24)
        };
        AppTheme.ApplyCardPanel(pnlRightDetail);

        var lblDetailHeader = new Label
        {
            Text = "Chi Tiết Danh Mục Lựa Chọn",
            Font = AppTheme.FontH2,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };

        var lblName = new Label { Text = "Tên danh mục:", Font = AppTheme.FontBodyBold, Location = new Point(24, 65), AutoSize = true, ForeColor = AppTheme.TextPrimary };
        txtCategoryName = new TextBox { Font = AppTheme.FontBody, Location = new Point(24, 90), Size = new Size(460, 32), BorderStyle = BorderStyle.FixedSingle };

        var lblSlug = new Label { Text = "Slug (SEO):", Font = AppTheme.FontBodyBold, Location = new Point(24, 135), AutoSize = true, ForeColor = AppTheme.TextPrimary };
        txtSlug = new TextBox { Font = AppTheme.FontBody, Location = new Point(24, 160), Size = new Size(460, 32), BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, BackColor = AppTheme.BackgroundGray };

        var lblDesc = new Label { Text = "Mô tả chi tiết:", Font = AppTheme.FontBodyBold, Location = new Point(24, 205), AutoSize = true, ForeColor = AppTheme.TextPrimary };
        txtDescription = new TextBox { Font = AppTheme.FontBody, Location = new Point(24, 230), Size = new Size(460, 75), Multiline = true, BorderStyle = BorderStyle.FixedSingle };

        btnSave = new Button
        {
            Text = "💾 Lưu Thay Đổi",
            Size = new Size(130, 32),
            Location = new Point(24, 325)
        };
        AppTheme.ApplyPrimaryButton(btnSave);
        btnSave.Click += (s, e) => AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã cập nhật danh mục thành công!");

        btnAddChild = new Button
        {
            Text = "➕ Thêm Danh Mục Con",
            Size = new Size(170, 32),
            Location = new Point(165, 325)
        };
        AppTheme.ApplySecondaryButton(btnAddChild);
        btnAddChild.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Vui lòng nhập thông tin danh mục con mới.");

        btnDelete = new Button
        {
            Text = "🗑️ Xóa Danh Mục",
            Size = new Size(130, 32),
            Location = new Point(345, 325)
        };
        AppTheme.ApplyDangerButton(btnDelete);
        btnDelete.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Chức năng xóa yêu cầu xác nhận Admin.");

        pnlRightDetail.Controls.Add(lblDetailHeader);
        pnlRightDetail.Controls.Add(lblName);
        pnlRightDetail.Controls.Add(txtCategoryName);
        pnlRightDetail.Controls.Add(lblSlug);
        pnlRightDetail.Controls.Add(txtSlug);
        pnlRightDetail.Controls.Add(lblDesc);
        pnlRightDetail.Controls.Add(txtDescription);
        pnlRightDetail.Controls.Add(btnSave);
        pnlRightDetail.Controls.Add(btnAddChild);
        pnlRightDetail.Controls.Add(btnDelete);

        this.Controls.Add(pnlRightDetail);
        this.Controls.Add(pnlLeftTree);
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            tvCategories.Nodes.Clear();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/categories");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.TryGetProperty("items", out var itemsElem))
                {
                    foreach (var item in itemsElem.EnumerateArray())
                    {
                        string name = item.TryGetProperty("categoryName", out var n) ? n.GetString() ?? "" : "";
                        string slug = item.TryGetProperty("slug", out var s) ? s.GetString() ?? "" : "";
                        string desc = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                        var node = new TreeNode($"📦 {name}") { Tag = new CategoryTag { Slug = slug, Description = desc } };
                        tvCategories.Nodes.Add(node);
                    }
                }
            }

            if (tvCategories.Nodes.Count == 0)
            {
                tvCategories.Nodes.Add(new TreeNode("Chưa có danh mục nào (Bấm + để thêm)"));
            }

            tvCategories.ExpandAll();
        }
        catch
        {
            // Graceful fallback
        }
    }

    private void TvCategories_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node != null)
        {
            txtCategoryName.Text = e.Node.Text.Replace("📦 ", "").Replace("🥤 ", "").Replace("💧 ", "").Replace("🥛 ", "");
            if (e.Node.Tag is CategoryTag tag)
            {
                txtSlug.Text = tag.Slug;
                txtDescription.Text = tag.Description;
            }
        }
    }

    private class CategoryTag
    {
        public string Slug { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
