using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Desktop.Services;

/// <summary>
/// Dịch vụ xuất Biên Bản Đối Soát Kiểm Kê & Hao Hụt Tài Chính định dạng PDF chuẩn in ấn A4 (QuestPDF).
/// Hỗ trợ in khổ A4 Landscape chuyên nghiệp kèm chữ ký xác nhận 3 bên.
/// </summary>
public static class StockAuditPdfService
{
    static StockAuditPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static bool GenerateStockAuditPdf(StockAuditDTO audit, string? targetFolder = null)
    {
        if (audit.Details == null || audit.Details.Count == 0)
        {
            MessageBox.Show("Đợt kiểm kê chưa có dữ liệu sản phẩm để in biên bản PDF!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        string auditCode = audit.Id.ToString()[..8].ToUpper();
        targetFolder ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        string safeTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(targetFolder, $"BienBanKiemKe_{auditCode}_{safeTime}.pdf");

        try
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(12, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9).FontColor(Colors.Grey.Darken3));

                    page.Header().Element(c => ComposeHeader(c, audit, auditCode));
                    page.Content().Element(c => ComposeContent(c, audit));
                    page.Footer().Element(c => ComposeFooter(c));
                });
            });

            document.GeneratePdf(filePath);

            var result = MessageBox.Show(
                $"Xuất Biên bản kiểm kê PDF thành công!\nĐường dẫn: {filePath}\n\nBạn có muốn mở file PDF ngay không?",
                "Xuất PDF Thành Công",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes && File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất file PDF: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static void ComposeHeader(IContainer container, StockAuditDTO audit, string auditCode)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("SMART SUPERMARKET - HỆ THỐNG QUẢN LÝ SIÊU THỊ THÔNG MINH")
                        .FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                    c.Item().Text("BIÊN BẢN ĐỐI SOÁT KIỂM KÊ KHO & HAO HỤT TÀI CHÍNH")
                        .FontSize(15).Bold().FontColor(Colors.Blue.Darken3);
                });

                row.ConstantItem(200).AlignRight().Column(c =>
                {
                    c.Item().Text($"Mã phiếu: #{auditCode}").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                    c.Item().Text($"Ngày kiểm: {audit.AuditDate:dd/MM/yyyy HH:mm}").FontSize(8.5f);
                    string statusDisplay = audit.Status switch
                    {
                        "Reconciled" => "Trạng thái: Đã cân bằng kho",
                        "Completed" => "Trạng thái: Chờ cân bằng kho",
                        _ => "Trạng thái: Đang kiểm đếm"
                    };
                    c.Item().Text(statusDisplay).Bold().FontSize(8.5f).FontColor(audit.Status == "Reconciled" ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                });
            });

            col.Item().PaddingTop(4).PaddingBottom(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"Khu vực kiểm: {audit.Zone}  |  Nhân viên kiểm: {audit.CreatedBy}  |  Người cân bằng: {audit.ReconciledBy ?? "---"}");
                r.ConstantItem(180).AlignRight().Text($"Thời gian in: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").Italic().FontSize(8);
            });

            col.Item().PaddingBottom(8);
        });
    }

    private static void ComposeContent(IContainer container, StockAuditDTO audit)
    {
        container.Column(col =>
        {
            // 1. KPI Cards Row
            int matchCount = audit.Details.Count(d => d.VarianceQty == 0);
            int diffCount = audit.Details.Count(d => d.VarianceQty != 0);

            col.Item().PaddingBottom(8).Row(r =>
            {
                r.RelativeItem().Background(Colors.Grey.Lighten4).Padding(6).Column(c =>
                {
                    c.Item().Text("Tổng SKU Kiểm").FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{audit.TotalItems} mặt hàng").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                });
                r.ConstantItem(8);

                r.RelativeItem().Background(Colors.Green.Lighten5).Padding(6).Column(c =>
                {
                    c.Item().Text("SKU Khớp Tồn").FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{matchCount} mặt hàng").Bold().FontSize(11).FontColor(Colors.Green.Darken2);
                });
                r.ConstantItem(8);

                r.RelativeItem().Background(Colors.Orange.Lighten5).Padding(6).Column(c =>
                {
                    c.Item().Text("SKU Lệch Tồn").FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{diffCount} mặt hàng").Bold().FontSize(11).FontColor(Colors.Orange.Darken3);
                });
                r.ConstantItem(8);

                r.RelativeItem().Background(Colors.Red.Lighten5).Padding(6).Column(c =>
                {
                    c.Item().Text("Tổng Tiền Hao Hụt").FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{audit.TotalLossAmount:#,##0 đ}").Bold().FontSize(11).FontColor(Colors.Red.Darken2);
                });
            });

            // 2. Data Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);  // STT
                    columns.ConstantColumn(85);  // Barcode
                    columns.RelativeColumn(3);   // Tên SP
                    columns.ConstantColumn(65);  // Đơn giá
                    columns.ConstantColumn(50);  // Tồn sổ
                    columns.ConstantColumn(50);  // Thực tế
                    columns.ConstantColumn(50);  // Lệch SL
                    columns.ConstantColumn(75);  // Thành tiền lệch
                    columns.ConstantColumn(50);  // Tình trạng
                    columns.RelativeColumn(2);   // Nguyên nhân hao hụt
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("STT").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("Mã Barcode").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignLeft().Text("Tên Sản Phẩm").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignRight().Text("Đơn Giá").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignRight().Text("Tồn Sổ").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignRight().Text("Thực Tế").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignRight().Text("Lệch SL").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignRight().Text("Tiền Lệch").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("Trạng Thái").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignLeft().Text("Nguyên Nhân Hao Hụt").Bold().FontColor(Colors.White);
                });

                int stt = 1;
                foreach (var item in audit.Details)
                {
                    var bg = stt % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                    if (item.VarianceQty < 0) bg = Colors.Red.Lighten5;
                    else if (item.VarianceQty > 0) bg = Colors.Orange.Lighten5;

                    string statusStr = item.VarianceQty == 0 ? "Khớp" : (item.VarianceQty < 0 ? "Thiếu" : "Thừa");
                    string reasonStr = !string.IsNullOrWhiteSpace(item.LossReason) ? item.LossReason : (item.VarianceQty == 0 ? "Khớp tồn" : "Chưa chọn");

                    table.Cell().Background(bg).Padding(3).AlignCenter().Text(stt.ToString());
                    table.Cell().Background(bg).Padding(3).AlignCenter().Text(item.Barcode);
                    table.Cell().Background(bg).Padding(3).AlignLeft().Text(item.ProductName);
                    table.Cell().Background(bg).Padding(3).AlignRight().Text($"{item.UnitPrice:#,##0 đ}");
                    table.Cell().Background(bg).Padding(3).AlignRight().Text(item.SystemQty.ToString());
                    table.Cell().Background(bg).Padding(3).AlignRight().Text(item.ActualQty.ToString());

                    var varCell = table.Cell().Background(bg).Padding(3).AlignRight();
                    string varPrefix = item.VarianceQty > 0 ? "+" : "";
                    if (item.VarianceQty < 0)
                        varCell.Text($"{varPrefix}{item.VarianceQty}").Bold().FontColor(Colors.Red.Darken2);
                    else if (item.VarianceQty > 0)
                        varCell.Text($"{varPrefix}{item.VarianceQty}").Bold().FontColor(Colors.Orange.Darken3);
                    else
                        varCell.Text("0");

                    var amtCell = table.Cell().Background(bg).Padding(3).AlignRight();
                    if (item.VarianceQty < 0)
                        amtCell.Text($"{item.VarianceAmount:#,##0 đ}").Bold().FontColor(Colors.Red.Darken2);
                    else
                        amtCell.Text($"{item.VarianceAmount:#,##0 đ}");

                    table.Cell().Background(bg).Padding(3).AlignCenter().Text(statusStr);
                    table.Cell().Background(bg).Padding(3).AlignLeft().Text(reasonStr);

                    stt++;
                }

                // Summary Row
                table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("TỔNG CỘNG").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{audit.TotalDiscrepancyQty}").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{audit.TotalLossAmount:#,##0 đ}").Bold().FontColor(Colors.Red.Darken2);
                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(4).Text("");
            });

            // 3. Signature Block
            col.Item().PaddingTop(24).Row(r =>
            {
                r.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("NGƯỜI LẬP BIÊN BẢN").Bold();
                    c.Item().Text("(Ký và ghi rõ họ tên)").Italic().FontSize(8);
                    c.Item().PaddingTop(40).Text(audit.CreatedBy).Bold();
                });

                r.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("THỦ KHO PHỤ TRÁCH").Bold();
                    c.Item().Text("(Ký và ghi rõ họ tên)").Italic().FontSize(8);
                    c.Item().PaddingTop(40).Text("......................................").FontSize(8);
                });

                r.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("CỬA HÀNG TRƯỞNG / KẾ TOÁN").Bold();
                    c.Item().Text("(Ký duyệt & đóng dấu)").Italic().FontSize(8);
                    c.Item().PaddingTop(40).Text(!string.IsNullOrWhiteSpace(audit.ReconciledBy) ? audit.ReconciledBy : "......................................").Bold();
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Smart SuperMarket ERP - Phân Hệ Quản Lý Kho & Kiểm Kê").FontSize(8).FontColor(Colors.Grey.Medium);
            row.ConstantItem(100).AlignRight().Text(text =>
            {
                text.Span("Trang ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }
}
