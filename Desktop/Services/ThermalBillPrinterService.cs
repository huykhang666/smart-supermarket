using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Desktop.Services;

public class ThermalBillPrinterService
{
    public static void PrintOrPreview(InvoicePrintModel model, bool directPrint = false, string printerName = "")
    {
        var pd = new PrintDocument();
        pd.DocumentName = $"Bill_{model.OrderCode}";

        if (!string.IsNullOrEmpty(printerName))
        {
            pd.PrinterSettings.PrinterName = printerName;
        }

        // Khổ K80: Rộng ~80mm (tương đương 315/100 inch hay ~80mm), chiều dài tự co giãn theo nội dung
        int paperWidth = 314; // ~80mm
        pd.DefaultPageSettings.PaperSize = new PaperSize("K80_80mm", paperWidth, 2000);
        pd.DefaultPageSettings.Margins = new Margins(10, 10, 15, 20);

        pd.PrintPage += (sender, e) =>
        {
            RenderThermalReceipt(e.Graphics!, model, e.MarginBounds.Left, e.MarginBounds.Top, e.MarginBounds.Width);
            e.HasMorePages = false;
        };

        if (directPrint && pd.PrinterSettings.IsValid)
        {
            pd.Print();
        }
        else
        {
            var previewDlg = new PrintPreviewDialog
            {
                Document = pd,
                Width = 480,
                Height = 720,
                StartPosition = FormStartPosition.CenterParent,
                Text = $"Xem Trước Hóa Đơn Nhiệt K80 - {model.OrderCode}"
            };
            previewDlg.ShowDialog();
        }
    }

    private static void RenderThermalReceipt(Graphics g, InvoicePrintModel model, int left, int top, int width)
    {
        using var fontStore = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var fontHeader = new Font("Segoe UI", 9f, FontStyle.Regular);
        using var fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fontRegular = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var fontSmall = new Font("Segoe UI", 7.5f, FontStyle.Regular);
        using var fontBarcode = new Font("Courier New", 14f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.Black);
        using var penDashed = new Pen(Color.Black, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        using var penDouble = new Pen(Color.Black, 1.5f);

        float y = top;
        var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
        var rightFormat = new StringFormat { Alignment = StringAlignment.Far };
        var leftFormat = new StringFormat { Alignment = StringAlignment.Near };

        // 1. Header Siêu Thị
        g.DrawString("SMART SUPERMARKET", fontStore, brush, new RectangleF(left, y, width, 22), centerFormat);
        y += 24;
        g.DrawString(model.BranchName, fontHeader, brush, new RectangleF(left, y, width, 18), centerFormat);
        y += 18;
        g.DrawString(model.BranchAddress, fontSmall, brush, new RectangleF(left, y, width, 16), centerFormat);
        y += 16;
        g.DrawString($"Hotline: {model.BranchPhone}", fontSmall, brush, new RectangleF(left, y, width, 16), centerFormat);
        y += 20;

        g.DrawLine(penDouble, left, y, left + width, y);
        y += 6;

        // 2. Tiêu đề Hóa Đơn
        g.DrawString("HÓA ĐƠN BÁN LẺ (K80)", fontTitle, brush, new RectangleF(left, y, width, 22), centerFormat);
        y += 24;

        g.DrawString($"Số HĐ: {model.OrderCode}", fontBold, brush, left, y);
        y += 18;
        g.DrawString($"Ngày: {model.OrderDate:dd/MM/yyyy HH:mm:ss}", fontRegular, brush, left, y);
        y += 17;
        g.DrawString($"Thu ngân: {model.CashierName}", fontRegular, brush, left, y);
        y += 17;

        if (!string.IsNullOrEmpty(model.CustomerName) && model.CustomerName != "Khách lẻ")
        {
            string phoneStr = !string.IsNullOrEmpty(model.CustomerPhone) ? $" ({model.CustomerPhone})" : "";
            g.DrawString($"Khách hàng: {model.CustomerName}{phoneStr}", fontRegular, brush, left, y);
            y += 18;
        }

        g.DrawLine(penDashed, left, y, left + width, y);
        y += 6;

        // 3. Tiêu đề Cột
        // Layout: [Tên SP / Đơn giá] [SL] [Thành tiền]
        float colQtyWidth = 35;
        float colTotalWidth = 85;
        float colNameWidth = width - colQtyWidth - colTotalWidth;

        g.DrawString("TÊN HÀNG", fontBold, brush, left, y);
        g.DrawString("SL", fontBold, brush, new RectangleF(left + colNameWidth, y, colQtyWidth, 18), rightFormat);
        g.DrawString("T.TIỀN", fontBold, brush, new RectangleF(left + width - colTotalWidth, y, colTotalWidth, 18), rightFormat);
        y += 18;

        g.DrawLine(penDashed, left, y, left + width, y);
        y += 6;

        // 4. Danh sách Mặt Hàng
        foreach (var item in model.Items)
        {
            // Dòng 1: Tên món
            var nameRect = new RectangleF(left, y, colNameWidth - 4, 34);
            g.DrawString(item.ProductName, fontRegular, brush, nameRect, leftFormat);

            // Cột SL và Thành tiền căn ngang với tên món
            g.DrawString(item.Quantity.ToString(), fontBold, brush, new RectangleF(left + colNameWidth, y, colQtyWidth, 18), rightFormat);
            g.DrawString($"{item.SubTotal:N0}đ", fontBold, brush, new RectangleF(left + width - colTotalWidth, y, colTotalWidth, 18), rightFormat);

            // Dòng phụ: Đơn giá x SL
            y += 17;
            g.DrawString($"  {item.Quantity} x {item.UnitPrice:N0}đ", fontSmall, brush, left, y);
            y += 16;
        }

        g.DrawLine(penDashed, left, y, left + width, y);
        y += 6;

        // 5. Phần Tổng Tiền
        DrawSummaryRow(g, fontRegular, fontBold, brush, left, ref y, width, "Tạm tính:", $"{model.SubTotal:N0} đ");
        
        if (model.DiscountTotal > 0)
        {
            DrawSummaryRow(g, fontRegular, fontBold, brush, left, ref y, width, "Giảm giá:", $"-{model.DiscountTotal:N0} đ");
        }

        if (model.VatTotal > 0)
        {
            DrawSummaryRow(g, fontRegular, fontRegular, brush, left, ref y, width, "VAT (ước tính 8%):", $"{model.VatTotal:N0} đ");
        }

        y += 2;
        g.DrawLine(penDouble, left, y, left + width, y);
        y += 6;

        // TỔNG CỘNG
        g.DrawString("TỔNG THANH TOÁN:", fontTitle, brush, left, y);
        g.DrawString($"{model.GrandTotal:N0} đ", fontTitle, brush, new RectangleF(left, y, width, 22), rightFormat);
        y += 24;

        g.DrawLine(penDashed, left, y, left + width, y);
        y += 6;

        DrawSummaryRow(g, fontRegular, fontRegular, brush, left, ref y, width, "Hình thức TT:", model.PaymentMethod);
        DrawSummaryRow(g, fontRegular, fontRegular, brush, left, ref y, width, "Tiền khách đưa:", $"{model.CashGiven:N0} đ");
        DrawSummaryRow(g, fontBold, fontBold, brush, left, ref y, width, "Tiền thối lại:", $"{model.ChangeDue:N0} đ");

        y += 8;
        g.DrawLine(penDashed, left, y, left + width, y);
        y += 10;

        // 6. Barcode Hóa Đơn Phục Vụ Đổi Trả / Tra Cứu
        DrawThermalBarcode(g, model.OrderCode, left, y, width);
        y += 45;
        g.DrawString($"*{model.OrderCode}*", fontBold, brush, new RectangleF(left, y, width, 18), centerFormat);
        y += 22;

        // 7. Lời Cảm Ơn & Ghi Chú Đổi Trả
        g.DrawString("CẢM ƠN QUÝ KHÁCH VÀ HẸN GẶP LẠI!", fontBold, brush, new RectangleF(left, y, width, 18), centerFormat);
        y += 18;
        g.DrawString("Quý khách vui lòng giữ hóa đơn trong 7 ngày\nđể được hỗ trợ đổi trả sản phẩm.", fontSmall, brush, new RectangleF(left, y, width, 32), centerFormat);
        y += 34;

        g.DrawString("- - - - - CẮT GIẤY TẠI ĐÂY - - - - -", fontSmall, brush, new RectangleF(left, y, width, 16), centerFormat);
    }

    private static void DrawSummaryRow(Graphics g, Font labelFont, Font valFont, Brush brush, int left, ref float y, int width, string label, string val)
    {
        g.DrawString(label, labelFont, brush, left, y);
        g.DrawString(val, valFont, brush, new RectangleF(left, y, width, 18), new StringFormat { Alignment = StringAlignment.Far });
        y += 18;
    }

    private static void DrawThermalBarcode(Graphics g, string code, int left, float y, int width)
    {
        // Vẽ mã vạch giả lập Code128 / Code39 bằng Graphics đơn giản, sắc nét
        using var blackBrush = new SolidBrush(Color.Black);
        using var whiteBrush = new SolidBrush(Color.White);

        int barHeight = 35;
        int totalBars = 65;
        int barWidth = 2;
        int barcodeTotalWidth = totalBars * barWidth;
        int startX = left + (width - barcodeTotalWidth) / 2;

        // Seed từ hashcode của mã đơn để các vạch sinh ra nhất quán theo mã
        int hash = Math.Abs(code.GetHashCode());
        var rand = new Random(hash);

        // Guard bars đầu
        g.FillRectangle(blackBrush, startX, y, barWidth, barHeight);
        g.FillRectangle(blackBrush, startX + barWidth * 2, y, barWidth, barHeight);

        for (int i = 4; i < totalBars - 4; i++)
        {
            if (rand.Next(0, 2) == 1)
            {
                g.FillRectangle(blackBrush, startX + i * barWidth, y, barWidth, barHeight);
            }
        }

        // Guard bars cuối
        g.FillRectangle(blackBrush, startX + (totalBars - 3) * barWidth, y, barWidth, barHeight);
        g.FillRectangle(blackBrush, startX + (totalBars - 1) * barWidth, y, barWidth, barHeight);
    }
}
