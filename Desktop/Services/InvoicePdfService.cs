using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Desktop.Services;

public class InvoicePrintItem
{
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Unit { get; set; } = "Cái";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal => Quantity * UnitPrice;
}

public class InvoicePrintModel
{
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = "Khách lẻ";
    public string CustomerPhone { get; set; } = "";
    public string CashierName { get; set; } = "Thu ngân";
    public string BranchName { get; set; } = "Smart SuperMarket - Chi nhánh 1 (Quận 1, TP.HCM)";
    public string BranchAddress { get; set; } = "123 Đường Công Nghệ, Quận 1, TP. Hồ Chí Minh";
    public string BranchPhone { get; set; } = "1900 6868 - 028 3822 9999";
    
    public decimal SubTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public decimal CashGiven { get; set; }
    public decimal ChangeDue { get; set; }
    
    public List<InvoicePrintItem> Items { get; set; } = new();
}

public static class InvoicePdfService
{
    static InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static string GenerateInvoicePdf(InvoicePrintModel model, string? targetFolder = null)
    {
        targetFolder ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Invoices");
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        string safeCode = string.IsNullOrWhiteSpace(model.OrderCode) ? $"HD-{DateTime.Now:yyMMddHHmmss}" : model.OrderCode.Replace(":", "-").Replace("/", "-");
        string filePath = Path.Combine(targetFolder, $"{safeCode}.pdf");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Khổ A5 gọn gàng, trang trọng chuẩn hóa đơn bán lẻ
                page.Size(PageSizes.A5);
                page.Margin(18, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9.5f).FontColor(Colors.Grey.Darken3));

                page.Header().Element(c => ComposeHeader(c, model));
                page.Content().Element(c => ComposeContent(c, model));
                page.Footer().Element(c => ComposeFooter(c, model));
            });
        });

        document.GeneratePdf(filePath);
        return filePath;
    }

    public static string PrintOrPreview(InvoicePrintModel model, bool autoOpen = true)
    {
        string filePath = GenerateInvoicePdf(model);

        if (autoOpen && File.Exists(filePath))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InvoicePdfService] Cannot open PDF automatically: {ex.Message}");
            }
        }

        return filePath;
    }

    private static void ComposeHeader(IContainer container, InvoicePrintModel model)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("SMART SUPERMARKET")
                .FontSize(17)
                .Bold()
                .FontColor("#0d6efd");

            col.Item().AlignCenter().Text(model.BranchAddress)
                .FontSize(8.5f)
                .FontColor(Colors.Grey.Darken1);

            col.Item().AlignCenter().Text($"Hotline: {model.BranchPhone}")
                .FontSize(8.5f)
                .FontColor(Colors.Grey.Darken1);

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().PaddingTop(6).AlignCenter().Text("HÓA ĐƠN BÁN LẺ (RETAIL INVOICE)")
                .FontSize(13)
                .Bold()
                .FontColor(Colors.Black);

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Mã hóa đơn: {model.OrderCode}").Bold().FontColor(Colors.Black);
                    c.Item().Text($"Khách hàng: {model.CustomerName}");
                    if (!string.IsNullOrEmpty(model.CustomerPhone))
                    {
                        c.Item().Text($"Số ĐT: {model.CustomerPhone}");
                    }
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignRight().Text($"Ngày: {model.OrderDate:dd/MM/yyyy HH:mm}");
                    c.Item().AlignRight().Text($"Thu ngân: {model.CashierName}");
                    c.Item().AlignRight().Text($"Hình thức: {model.PaymentMethod}").Bold();
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ComposeContent(IContainer container, InvoicePrintModel model)
    {
        container.PaddingTop(6).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(24);              // STT
                    columns.RelativeColumn(5);               // Tên SP
                    columns.ConstantColumn(36);              // ĐVT
                    columns.ConstantColumn(32);              // SL
                    columns.RelativeColumn(2.5f);            // Đơn giá
                    columns.RelativeColumn(2.8f);            // Thành tiền
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).Text("#").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).Text("Tên Sản Phẩm").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).AlignCenter().Text("ĐVT").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).AlignCenter().Text("SL").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).AlignRight().Text("Đơn Giá").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).AlignRight().Text("Thành Tiền").Bold();
                });

                int stt = 1;
                foreach (var item in model.Items)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Text(stt.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Text(item.ProductName).SemiBold();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).AlignCenter().Text(item.Unit);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).AlignCenter().Text(item.Quantity.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).AlignRight().Text($"{item.UnitPrice:N0} đ");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).AlignRight().Text($"{item.SubTotal:N0} đ");
                    stt++;
                }
            });

            // Tổng kết hóa đơn
            col.Item().PaddingTop(8).AlignRight().Width(240).Column(summary =>
            {
                summary.Item().Row(r =>
                {
                    r.RelativeItem().Text("Tạm tính:");
                    r.RelativeItem().AlignRight().Text($"{model.SubTotal:N0} đ");
                });

                if (model.DiscountTotal > 0)
                {
                    summary.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Chiết khấu / Giảm giá:").FontColor(Colors.Red.Medium);
                        r.RelativeItem().AlignRight().Text($"-{model.DiscountTotal:N0} đ").FontColor(Colors.Red.Medium);
                    });
                }

                if (model.VatTotal > 0)
                {
                    summary.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Thuế VAT (8%):");
                        r.RelativeItem().AlignRight().Text($"{model.VatTotal:N0} đ");
                    });
                }

                summary.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken1);

                summary.Item().Row(r =>
                {
                    r.RelativeItem().Text("TỔNG THANH TOÁN:").Bold().FontSize(11).FontColor(Colors.Black);
                    r.RelativeItem().AlignRight().Text($"{model.GrandTotal:N0} VNĐ").Bold().FontSize(12).FontColor("#0d6efd");
                });

                if (model.CashGiven > 0)
                {
                    summary.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Tiền khách đưa:");
                        r.RelativeItem().AlignRight().Text($"{model.CashGiven:N0} đ");
                    });

                    summary.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Tiền trả lại:");
                        r.RelativeItem().AlignRight().Text($"{model.ChangeDue:N0} đ").Bold().FontColor(Colors.Green.Darken2);
                    });
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container, InvoicePrintModel model)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(4).AlignCenter().Text("Quý khách vui lòng kiểm tra lại hàng hóa và hóa đơn trước khi rời quầy.")
                .Italic().FontSize(8).FontColor(Colors.Grey.Darken1);
            col.Item().AlignCenter().Text("Hàng hóa được đổi trả trong vòng 07 ngày kèm theo hóa đơn này.")
                .Italic().FontSize(8).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(3).AlignCenter().Text("CẢM ƠN QUÝ KHÁCH & HẸN GẶP LẠI!")
                .Bold().FontSize(9.5f).FontColor(Colors.Black);
        });
    }
}
