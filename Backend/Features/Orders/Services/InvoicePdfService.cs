using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartSupermarket.Backend.Features.Orders.DTOs;

namespace SmartSupermarket.Backend.Features.Orders.Services;

public class InvoicePdfService : IInvoicePdfService
{
    static InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public byte[] GenerateInvoicePdf(OrderDto order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(12, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9.5f).FontColor(Colors.Grey.Darken3));

                page.Header().Element(c => ComposeHeader(c, order));
                page.Content().Element(c => ComposeContent(c, order));
                page.Footer().Element(c => ComposeFooter(c, order));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, OrderDto order)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("SMART SUPERMARKET")
                .FontSize(16)
                .Bold()
                .FontColor("#0d6efd");

            col.Item().AlignCenter().Text("123 Đường Công Nghệ, Quận 1, TP. Hồ Chí Minh")
                .FontSize(8.5f)
                .FontColor(Colors.Grey.Darken1);

            col.Item().AlignCenter().Text("Hotline: 1900 6868 - 028 3822 9999")
                .FontSize(8.5f)
                .FontColor(Colors.Grey.Darken1);

            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().PaddingTop(6).AlignCenter().Text("HÓA ĐƠN BÁN LẺ (RETAIL INVOICE)")
                .FontSize(12.5f)
                .Bold()
                .FontColor(Colors.Black);

            string orderCode = $"HD-{order.OrderId:D6}";

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Mã hóa đơn: {orderCode}").Bold().FontColor(Colors.Black);
                    c.Item().Text($"Khách hàng: {order.CustomerName ?? "Khách lẻ"}");
                    if (!string.IsNullOrEmpty(order.CustomerPhone))
                    {
                        c.Item().Text($"Số ĐT: {order.CustomerPhone}");
                    }
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignRight().Text($"Ngày: {order.OrderDate:dd/MM/yyyy HH:mm}");
                    c.Item().AlignRight().Text($"Thu ngân: {order.EmployeeName ?? $"NV #{order.EmployeeId}"}");
                    c.Item().AlignRight().Text($"Hình thức: {FormatPaymentMethod(order.PaymentMethod)}").Bold();
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ComposeContent(IContainer container, OrderDto order)
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
                foreach (var item in order.OrderDetails)
                {
                    string unit = string.IsNullOrWhiteSpace(item.Unit) ? "Cái" : item.Unit;
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Text(stt.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Text(item.ProductName).SemiBold();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).AlignCenter().Text(unit);
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
                    r.RelativeItem().AlignRight().Text($"{order.TotalAmount:N0} đ");
                });

                if (order.DiscountAmount > 0)
                {
                    summary.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Chiết khấu / Giảm giá:").FontColor(Colors.Red.Medium);
                        r.RelativeItem().AlignRight().Text($"-{order.DiscountAmount:N0} đ").FontColor(Colors.Red.Medium);
                    });
                }

                summary.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken1);

                summary.Item().Row(r =>
                {
                    r.RelativeItem().Text("TỔNG THANH TOÁN:").Bold().FontSize(11).FontColor(Colors.Black);
                    r.RelativeItem().AlignRight().Text($"{order.FinalAmount:N0} VNĐ").Bold().FontSize(12).FontColor("#0d6efd");
                });

                if (order.Payments != null && order.Payments.Any())
                {
                    decimal totalPaid = order.Payments.Sum(p => p.AmountPaid);
                    summary.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Đã thanh toán:");
                        r.RelativeItem().AlignRight().Text($"{totalPaid:N0} đ");
                    });

                    if (totalPaid > order.FinalAmount)
                    {
                        decimal change = totalPaid - order.FinalAmount;
                        summary.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Tiền thối lại:");
                            r.RelativeItem().AlignRight().Text($"{change:N0} đ").Bold().FontColor(Colors.Green.Darken2);
                        });
                    }
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container, OrderDto order)
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

    private static string FormatPaymentMethod(Domain.Enums.PaymentMethod method)
    {
        return method switch
        {
            Domain.Enums.PaymentMethod.Cash => "Tiền mặt",
            Domain.Enums.PaymentMethod.QRCode => "Chuyển khoản (VietQR)",
            Domain.Enums.PaymentMethod.CreditCard => "Thẻ ngân hàng",
            _ => method.ToString()
        };
    }
}
