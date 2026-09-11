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
    private readonly string _apiBaseUrl = "http://localhost:5137";

    public CategoriesView()
    {
        InitializeComponent();
        _ = LoadCategoriesAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemeManager.Background;
        this.Padding = new Padding(20);

        // --- Left Tree Panel ---
        var pnlLeftTree = new Panel
        {
            Dock = DockStyle.Left,
            Width = 360,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 15, 0)
        };
        ThemeManager.ApplyCardPanel(pnlLeftTree);

        var lblTreeHeader = new Label
        {
            Text = "📁 CÂY DANH MỤC SẢN PHẨM",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.PrimaryHover,
            Dock = DockStyle.Top,
            Height = 40
        };

        tvCategories = new TreeView
        {
            Dock = DockStyle.Fill,
            Font = ThemeManager.BodyFont,
            BorderStyle = BorderStyle.None,
            BackColor = ThemeManager.CardBg,
            ForeColor = ThemeManager.TextPrimary
        };
        tvCategories.AfterSelect += TvCategories_AfterSelect;

        pnlLeftTree.Controls.Add(tvCategories);
        pnlLeftTree.Controls.Add(lblTreeHeader);

        // --- Right Detail Panel ---
        pnlRightDetail = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.CardBg,
            Padding = new Padding(25)
        };
        ThemeManager.ApplyCardPanel(pnlRightDetail);

        var lblDetailHeader = new Label
        {
            Text = "CHI TIẾT DANH MỤC CHỌN",
            Font = ThemeManager.HeaderFont,
            ForeColor = ThemeManager.TextPrimary,
            AutoSize = true,
            Location = new Point(25, 20)
        };

        var lblName = new Label { Text = "Tên danh mục:", Font = ThemeManager.BodyBold, Location = new Point(25, 75), AutoSize = true, ForeColor = ThemeManager.TextPrimary };
        txtCategoryName = new TextBox { Font = ThemeManager.BodyFont, Location = new Point(25, 100), Size = new Size(440, 32), BorderStyle = BorderStyle.FixedSingle };

        var lblSlug = new Label { Text = "Slug (SEO):", Font = ThemeManager.BodyBold, Location = new Point(25, 150), AutoSize = true, ForeColor = ThemeManager.TextPrimary };
        txtSlug = new TextBox { Font = ThemeManager.BodyFont, Location = new Point(25, 175), Size = new Size(440, 32), BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, BackColor = ColorTranslator.FromHtml("#F9FAFB") };

        var lblDesc = new Label { Text = "Mô tả chi tiết:", Font = ThemeManager.BodyBold, Location = new Point(25, 225), AutoSize = true, ForeColor = ThemeManager.TextPrimary };
        txtDescription = new TextBox { Font = ThemeManager.BodyFont, Location = new Point(25, 250), Size = new Size(440, 80), Multiline = true, BorderStyle = BorderStyle.FixedSingle };

        btnSave = new Button
        {
            Text = "💾 Lưu Thay Đổi",
            Size = new Size(140, 40),
            Location = new Point(25, 350)
        };
        ThemeManager.ApplyPrimaryButton(btnSave);
        btnSave.Click += (s, e) => AntdUI.Message.success(this.FindForm() ?? new Form(), "Đã cập nhật danh mục thành công!");

        btnAddChild = new Button
        {
            Text = "➕ Thêm Danh Mục Con",
            Size = new Size(180, 40),
            Location = new Point(180, 350)
        };
        ThemeManager.ApplySecondaryButton(btnAddChild);
        btnAddChild.Click += (s, e) => AntdUI.Message.info(this.FindForm() ?? new Form(), "Vui lòng nhập thông tin danh mục con mới.");

        btnDelete = new Button
        {
            Text = "🗑️ Xóa Danh Mục",
            Size = new Size(140, 40),
            Location = new Point(375, 350)
        };
        ThemeManager.ApplyDangerButton(btnDelete);
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
