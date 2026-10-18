using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace Desktop.Services;

public class OrderExportRow
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public string CustomerName { get; set; } = "Khách Lẻ";
    public string EmployeeName { get; set; } = "-";
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Tiền Mặt";
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Hoàn Thành";
}

public static class ExcelExportService
{
    /// <summary>
    /// Export a list of orders to an Excel file with professional formatting.
    /// Reports progress from 0 to 100 via the progress callback.
    /// </summary>
    public static async Task<string> ExportOrdersToExcelAsync(
        List<OrderExportRow> orders,
        string filePath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Danh Sách Đơn Hàng");

            progress?.Report(5);

            // === TITLE ===
            ws.Cell(1, 1).Value = "BÁO CÁO DANH SÁCH ĐƠN HÀNG";
            ws.Range(1, 1, 1, 9).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Font.SetFontColor(XLColor.FromHtml("#0B2545"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 9).Merge().Style
                .Font.SetItalic(true)
                .Font.SetFontSize(10)
                .Font.SetFontColor(XLColor.Gray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            progress?.Report(10);

            // === HEADERS (Row 4) ===
            string[] headers = { "STT", "Mã Đơn", "Khách Hàng", "Thu Ngân", "Tạm Tính (₫)", "Giảm Giá (₫)", "Thành Tiền (₫)", "Thanh Toán", "Ngày Đặt", "Trạng Thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style
                    .Font.SetBold(true)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#0087E6"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.FromHtml("#005FB4"));
            }

            progress?.Report(20);

            // === DATA ROWS ===
            int totalRows = orders.Count;
            for (int i = 0; i < totalRows; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var order = orders[i];
                int row = 5 + i;

                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = order.OrderCode;
                ws.Cell(row, 3).Value = order.CustomerName;
                ws.Cell(row, 4).Value = order.EmployeeName;
                ws.Cell(row, 5).Value = order.TotalAmount;
                ws.Cell(row, 6).Value = order.DiscountAmount;
                ws.Cell(row, 7).Value = order.FinalAmount;
                ws.Cell(row, 8).Value = order.PaymentMethod;
                ws.Cell(row, 9).Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 10).Value = order.Status;

                // Zebra striping
                if (i % 2 == 1)
                {
                    ws.Range(row, 1, row, 10).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EBF6FF"));
                }

                // Border
                ws.Range(row, 1, row, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.FromHtml("#BCE0FD"));
                ws.Range(row, 1, row, 10).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorderColor(XLColor.FromHtml("#BCE0FD"));

                // Number format for currency columns
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";

                // Alignment
                ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                ws.Cell(row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                ws.Cell(row, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                ws.Cell(row, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Progress: 20% to 80% for data writing
                int pct = 20 + (int)(60.0 * (i + 1) / totalRows);
                progress?.Report(pct);
            }

            // === SUMMARY ROW ===
            int summaryRow = 5 + totalRows + 1;
            ws.Cell(summaryRow, 1).Value = "TỔNG CỘNG";
            ws.Range(summaryRow, 1, summaryRow, 4).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Cell(summaryRow, 5).FormulaA1 = $"SUM(E5:E{4 + totalRows})";
            ws.Cell(summaryRow, 6).FormulaA1 = $"SUM(F5:F{4 + totalRows})";
            ws.Cell(summaryRow, 7).FormulaA1 = $"SUM(G5:G{4 + totalRows})";

            ws.Cell(summaryRow, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(summaryRow, 6).Style.NumberFormat.Format = "#,##0";
            ws.Cell(summaryRow, 7).Style.NumberFormat.Format = "#,##0";

            ws.Range(summaryRow, 1, summaryRow, 10).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#D4EBFA"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                .Border.SetOutsideBorderColor(XLColor.FromHtml("#0087E6"));

            ws.Cell(summaryRow + 1, 7).Value = $"Tổng: {totalRows} đơn hàng";
            ws.Cell(summaryRow + 1, 7).Style.Font.SetItalic(true).Font.SetFontColor(XLColor.Gray);

            progress?.Report(85);

            // === AUTO-FIT COLUMNS ===
            ws.Columns().AdjustToContents();
            // Minimum widths
            if (ws.Column(1).Width < 6) ws.Column(1).Width = 6;
            if (ws.Column(2).Width < 14) ws.Column(2).Width = 14;
            if (ws.Column(3).Width < 18) ws.Column(3).Width = 18;
            if (ws.Column(9).Width < 18) ws.Column(9).Width = 18;

            progress?.Report(90);

            // === SAVE ===
            workbook.SaveAs(filePath);

            progress?.Report(100);

            return filePath;
        }, cancellationToken);
    }

    /// <summary>
    /// Export promotion/voucher data to Excel
    /// </summary>
    public static async Task<string> ExportPromotionsToExcelAsync(
        List<Dictionary<string, object>> promotions,
        string filePath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Danh Sách Khuyến Mãi");

            progress?.Report(5);

            ws.Cell(1, 1).Value = "BÁO CÁO CHƯƠNG TRÌNH KHUYẾN MÃI";
            ws.Range(1, 1, 1, 7).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Font.SetFontColor(XLColor.FromHtml("#0B2545"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 7).Merge().Style
                .Font.SetItalic(true)
                .Font.SetFontSize(10)
                .Font.SetFontColor(XLColor.Gray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            progress?.Report(10);

            string[] headers = { "STT", "Mã Voucher", "Mô Tả", "Loại Giảm", "Giá Trị", "Ngày Bắt Đầu", "Ngày Kết Thúc", "Trạng Thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style
                    .Font.SetBold(true)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#0087E6"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            progress?.Report(20);

            for (int i = 0; i < promotions.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var promo = promotions[i];
                int row = 5 + i;

                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = promo.TryGetValue("Code", out var code) ? code?.ToString() ?? "" : "";
                ws.Cell(row, 3).Value = promo.TryGetValue("Description", out var desc) ? desc?.ToString() ?? "" : "";
                ws.Cell(row, 4).Value = promo.TryGetValue("DiscountType", out var dtype) ? dtype?.ToString() ?? "" : "";
                ws.Cell(row, 5).Value = promo.TryGetValue("DiscountValue", out var dval) ? dval?.ToString() ?? "" : "";
                ws.Cell(row, 6).Value = promo.TryGetValue("StartDate", out var sd) ? sd?.ToString() ?? "" : "";
                ws.Cell(row, 7).Value = promo.TryGetValue("EndDate", out var ed) ? ed?.ToString() ?? "" : "";
                ws.Cell(row, 8).Value = promo.TryGetValue("Status", out var st) ? st?.ToString() ?? "" : "";

                if (i % 2 == 1)
                    ws.Range(row, 1, row, 8).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EBF6FF"));

                ws.Range(row, 1, row, 8).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.FromHtml("#BCE0FD"));

                int pct = 20 + (int)(60.0 * (i + 1) / promotions.Count);
                progress?.Report(pct);
            }

            progress?.Report(85);
            ws.Columns().AdjustToContents();
            progress?.Report(90);
            workbook.SaveAs(filePath);
            progress?.Report(100);

            return filePath;
        }, cancellationToken);
    }
}
