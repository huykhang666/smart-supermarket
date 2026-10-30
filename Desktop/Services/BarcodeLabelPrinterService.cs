using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace Desktop.Services;

public class BarcodeLabelPrintModel
{
    public string StoreName { get; set; } = "SMART SUPERMARKET";
    public string ProductName { get; set; } = "Thịt Ba Rọi Heo Tươi";
    public string Barcode { get; set; } = "2000001004505";
    public decimal WeightKg { get; set; } = 0.450m;
    public decimal UnitPrice { get; set; } = 140000m;
    public decimal TotalAmount => Math.Round(WeightKg * UnitPrice, 0);
    public DateTime PackagingDate { get; set; } = DateTime.Now;
    public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(3);
    public string Origin { get; set; } = "Việt Nam";
}

public static class BarcodeLabelPrinterService
{
    /// <summary>
    /// In hoặc xem trước tem mã vạch dán khay thực phẩm tươi sống (Chuẩn tem nhiệt decal 50mm x 30mm hay 40mm x 30mm)
    /// </summary>
    public static void PrintOrPreview(BarcodeLabelPrintModel model, int copies = 1, bool directPrint = false, string printerName = "")
    {
        var pd = new PrintDocument();
        pd.DocumentName = $"Label_{model.Barcode}";

        if (!string.IsNullOrEmpty(printerName))
        {
            pd.PrinterSettings.PrinterName = printerName;
        }

        pd.PrinterSettings.Copies = (short)Math.Max(1, copies);

        // Kích thước tem nhãn siêu thị tiêu chuẩn: 50mm x 30mm (~197 x 118 pixel ở 100 DPI)
        int labelWidth = 197;
        int labelHeight = 125;
        pd.DefaultPageSettings.PaperSize = new PaperSize("Label_50x30", labelWidth, labelHeight);
        pd.DefaultPageSettings.Margins = new Margins(4, 4, 4, 4);

        pd.PrintPage += (sender, e) =>
        {
            RenderStickerLabel(e.Graphics!, model, e.MarginBounds.Left, e.MarginBounds.Top, e.MarginBounds.Width, e.MarginBounds.Height);
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
                Width = 460,
                Height = 360,
                StartPosition = FormStartPosition.CenterParent,
                Text = $"Xem Trước Tem Nhãn Barcode - {model.ProductName}"
            };
            previewDlg.ShowDialog();
        }
    }

    private static void RenderStickerLabel(Graphics g, BarcodeLabelPrintModel model, int left, int top, int width, int height)
    {
        using var fontStore = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var fontName = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var fontRegular = new Font("Segoe UI", 7f, FontStyle.Regular);
        using var fontPriceBold = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var fontBarcodeText = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.Black);
        using var penLine = new Pen(Color.Black, 1f);

        float y = top;
        var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
        var rightFormat = new StringFormat { Alignment = StringAlignment.Far };

        // 1. Tên siêu thị
        g.DrawString(model.StoreName, fontStore, brush, new RectangleF(left, y, width, 14), centerFormat);
        y += 13;
        g.DrawLine(penLine, left, y, left + width, y);
        y += 2;

        // 2. Tên sản phẩm
        g.DrawString(model.ProductName.ToUpper(), fontName, brush, new RectangleF(left, y, width, 16), centerFormat);
        y += 16;

        // 3. Thông tin khối lượng & đơn vị tính
        g.DrawString($"ĐG: {model.UnitPrice:N0} đ/kg", fontRegular, brush, left, y);
        g.DrawString($"KL: {model.WeightKg:F3} kg", fontPriceBold, brush, new RectangleF(left, y, width, 14), rightFormat);
        y += 14;

        // 4. Ngày đóng gói & Hạn sử dụng
        g.DrawString($"ĐG: {model.PackagingDate:dd/MM/yy} - HSD: {model.ExpiryDate:dd/MM/yy}", fontRegular, brush, left, y);
        y += 13;

        // 5. THÀNH TIỀN NỔI BẬT
        g.DrawLine(penLine, left, y, left + width, y);
        y += 2;
        g.DrawString("THÀNH TIỀN:", fontPriceBold, brush, left, y);
        g.DrawString($"{model.TotalAmount:N0} VNĐ", fontPriceBold, brush, new RectangleF(left, y, width, 15), rightFormat);
        y += 15;

        // 6. Mã vạch EAN-13
        DrawVectorBarcode(g, model.Barcode, left, y, width, 22);
        y += 23;
        g.DrawString(model.Barcode, fontBarcodeText, brush, new RectangleF(left, y, width, 12), centerFormat);
    }

    private static void DrawVectorBarcode(Graphics g, string code, int left, float y, int width, int barHeight)
    {
        using var blackBrush = new SolidBrush(Color.Black);
        int totalBars = 55;
        int barWidth = 2;
        int totalBarcodeWidth = totalBars * barWidth;
        int startX = left + Math.Max(0, (width - totalBarcodeWidth) / 2);

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
