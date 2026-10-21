using System;
using System.Diagnostics;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Desktop.Services;

public class ShiftReportPrintModel
{
    public string ReportDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
    public string ShiftName { get; set; } = "Ca Chiều - Tối (14:00 - 22:00)";
    public string CashierName { get; set; } = "Nhân viên Thu ngân (Staff POS)";
    public string BranchName { get; set; } = "Smart SuperMarket - Chi nhánh Quận 1, TP.HCM";
    public string BranchAddress { get; set; } = "123 Đường Công Nghệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh";
    public string BranchPhone { get; set; } = "Hotline: 1900 6868 | Quầy POS: (028) 3822 9999";
    
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    
    public decimal CashAmount { get; set; }
    public decimal QrAmount { get; set; }
    public decimal CardAmount { get; set; }
    
    public decimal OpeningCash { get; set; } = 2000000m; // Tiền quỹ đầu ca
    public decimal ClosingCash => OpeningCash + CashAmount; // Tiền két lúc kết ca
}

public static class ShiftReportPdfService
{
    static ShiftReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static string GenerateShiftReportPdf(ShiftReportPrintModel model, string? targetFolder = null)
    {
        targetFolder ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        string safeTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(targetFolder, $"ZReport_{safeTime}.pdf");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(c => ComposeHeader(c, model));
                page.Content().Element(c => ComposeContent(c, model));
                page.Footer().Element(c => ComposeFooter(c, model));
            });
        });

        document.GeneratePdf(filePath);
        return filePath;
    }

    public static string PrintOrPreview(ShiftReportPrintModel model, bool autoOpen = true)
    {
        string filePath = GenerateShiftReportPdf(model);

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
                Debug.WriteLine($"[ShiftReportPdfService] Cannot open PDF automatically: {ex.Message}");
            }
        }

        return filePath;
    }

    private static void ComposeHeader(IContainer container, ShiftReportPrintModel model)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(sub =>
                {
                    sub.Item().Text("SMART SUPERMARKET POS SYSTEM").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                    sub.Item().Text(model.BranchName).FontSize(9.5f).Bold();
                    sub.Item().Text(model.BranchAddress).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    sub.Item().Text(model.BranchPhone).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(180).Column(sub =>
                {
                    sub.Item().AlignRight().Text("MẪU SỐ: ERP-POS-Z01").FontSize(8.5f).Italic().FontColor(Colors.Grey.Darken1);
                    sub.Item().AlignRight().Text($"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    sub.Item().AlignRight().Text($"Mã phiên: POS-{DateTime.Now:yyMMddHHmm}").FontSize(8.5f).Bold().FontColor(Colors.Blue.Darken2);
                });
            });

            col.Item().PaddingTop(10).PaddingBottom(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().PaddingTop(6).PaddingBottom(12).AlignCenter().Column(titleCol =>
            {
                titleCol.Item().AlignCenter().Text("BIÊN BẢN KẾT CA BÁN HÀNG & BÀN GIAO KÉT TIỀN").FontSize(16).Bold().FontColor(Colors.Blue.Darken4);
                titleCol.Item().AlignCenter().Text("(BÁO CÁO TỔNG HỢP CUỐI CA - Z REPORT)").FontSize(10).Italic().FontColor(Colors.Grey.Darken2);
            });

            // Card thông tin ca trực
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Ngày bán hàng: {model.ReportDate}").Bold();
                    c.Item().Text($"Ca làm việc: {model.ShiftName}").Bold().FontColor(Colors.Blue.Darken3);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Thu ngân trực ca: {model.CashierName}").Bold();
                    c.Item().Text($"Trạng thái két: Đã chốt sổ ca").FontColor(Colors.Green.Darken2).Bold();
                });
            });
        });
    }

    private static void ComposeContent(IContainer container, ShiftReportPrintModel model)
    {
        container.PaddingTop(16).Column(col =>
        {
            // --- 1. TỔNG QUAN DOANH THU CA ---
            col.Item().Text("1. TỔNG QUAN KẾT QUẢ KINH DOANH CA").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
            col.Item().PaddingTop(4).PaddingBottom(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Text("Tổng số đơn hoàn tất:");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{model.TotalOrders} đơn").Bold();

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Text("Tổng tiền chiết khấu / KM:");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{model.TotalDiscount:N0} đ").FontColor(Colors.Red.Darken2);

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Text("Giá trị TB / đơn hàng:");
                decimal avg = model.TotalOrders > 0 ? Math.Round(model.TotalRevenue / model.TotalOrders, 0) : 0;
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{avg:N0} đ");

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Text("DOANH SỐ THỰC THU:");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Blue.Lighten5).Padding(6).AlignRight().Text($"{model.TotalRevenue:N0} đ").Bold().FontColor(Colors.Blue.Darken3);
            });

            // --- 2. PHÂN BỔ THEO PHƯƠNG THỨC THANH TOÁN ---
            col.Item().Text("2. DOANH THU THEO HÌNH THỨC THANH TOÁN").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
            col.Item().PaddingTop(4).PaddingBottom(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                // Header
                table.Header(h =>
                {
                    h.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignCenter().Text("#").FontColor(Colors.White).Bold();
                    h.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Phương Thức Thanh Toán").FontColor(Colors.White).Bold();
                    h.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignRight().Text("Số Tiền Thu Được").FontColor(Colors.White).Bold();
                    h.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignRight().Text("Tỷ Trọng (%)").FontColor(Colors.White).Bold();
                });

                decimal totalRev = model.TotalRevenue > 0 ? model.TotalRevenue : 1;

                // Tiền mặt
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter().Text("1");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("💵 Tiền mặt (Cash at Register)").Bold();
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{model.CashAmount:N0} đ").Bold();
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{(model.CashAmount / totalRev * 100):N1}%");

                // QR Code
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter().Text("2");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("📱 Chuyển khoản QR (VietQR / MoMo / VNPay)");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{model.QrAmount:N0} đ");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{(model.QrAmount / totalRev * 100):N1}%");

                // Thẻ ngân hàng
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter().Text("3");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("💳 Quẹt thẻ POS (ATM / Visa / Mastercard)");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{model.CardAmount:N0} đ");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{(model.CardAmount / totalRev * 100):N1}%");

                // Tổng cộng
                table.Cell().ColumnSpan(2).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Text("TỔNG DOANH THU CA:").Bold();
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).AlignRight().Text($"{model.TotalRevenue:N0} đ").Bold().FontColor(Colors.Blue.Darken3);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).AlignRight().Text("100.0%").Bold();
            });

            // --- 3. ĐỐI SOÁT KÉT TIỀN MẶT BÀN GIAO ---
            col.Item().Text("3. ĐỐI SOÁT & KIỂM KÊ KÉT TIỀN MẶT BÀN GIAO").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
            col.Item().PaddingTop(4).PaddingBottom(16).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(5);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(4);
                });

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Text("Hạng Mục Đối Soát").Bold();
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).AlignRight().Text("Số Tiền").Bold();
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Text("Ghi Chú").Bold();

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("A. Tiền lẻ đầu ca nhận bàn giao (Quỹ tiền mặt)");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{model.OpeningCash:N0} đ");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("Tiền lẻ thối lại cho khách");

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("B. Tiền mặt bán hàng thực thu trong ca");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{model.CashAmount:N0} đ").Bold();
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("Doanh số tiền mặt ghi nhận hệ thống");

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Green.Lighten5).Padding(6).Text("C. TỔNG TIỀN MẶT KẾT CA TRONG KÉT (A + B)").Bold();
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Green.Lighten5).Padding(6).AlignRight().Text($"{model.ClosingCash:N0} đ").Bold().FontColor(Colors.Green.Darken3);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Green.Lighten5).Padding(6).Text("Số tiền mặt bàn giao thực tế").Italic();

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("D. Chênh lệch kiểm kê két thực tế");
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text("0 đ").FontColor(Colors.Green.Darken2).Bold();
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("Khớp két 100%").FontColor(Colors.Green.Darken2);
            });

            // --- 4. CHỮ KÝ XÁC NHẬN ---
            col.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("THU NGÂN BÀN GIAO").Bold().FontSize(10.5f);
                    c.Item().AlignCenter().Text("(Ký và ghi rõ họ tên)").Italic().FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    c.Item().Height(55); // Khoảng trống ký tên
                    c.Item().AlignCenter().Text(model.CashierName).Bold().FontColor(Colors.Blue.Darken3);
                });

                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("QUẢN LÝ CA TIẾP NHẬN").Bold().FontSize(10.5f);
                    c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên và kiểm đếm)").Italic().FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    c.Item().Height(55); // Khoảng trống ký tên
                    c.Item().AlignCenter().Text("Quản lý Ca Bán Hàng").Bold().FontColor(Colors.Blue.Darken3);
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, ShiftReportPrintModel model)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("Hệ thống quản lý chuỗi siêu thị thông minh Smart SuperMarket - Phân hệ Báo cáo Doanh thu & Kết ca POS").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                row.ConstantItem(120).AlignRight().Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }
}
