using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace Desktop.Services;

public static class ExcelExportService
{
    /// <summary>
    /// Xuất dữ liệu từ DataGridView ra file Excel (.xlsx) với định dạng bảng biểu chuyên nghiệp.
    /// </summary>
    public static bool ExportDataGridViewToExcel(DataGridView dgv, string reportTitle, string defaultFileName)
    {
        if (dgv.Rows.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để xuất Excel!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            Title = $"Lưu Báo Cáo: {reportTitle}"
        };

        if (sfd.ShowDialog() != DialogResult.OK)
            return false;

        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Báo Cáo");

            // 1. Tiêu đề Báo Cáo
            worksheet.Cell(1, 1).Value = "SMART SUPERMARKET - ENTERPRISE ERP SYSTEM";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 11;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");

            worksheet.Cell(2, 1).Value = reportTitle.ToUpper();
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 16;
            worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

            worksheet.Cell(3, 1).Value = $"Thời gian xuất file: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Người xuất: {CurrentSession.FullName} ({CurrentSession.Role})";
            worksheet.Cell(3, 1).Style.Font.Italic = true;
            worksheet.Cell(3, 1).Style.Font.FontSize = 9.5;
            worksheet.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");

            // 2. Viết Header của Bảng
            int startRow = 5;
            int colIndex = 1;

            var visibleCols = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Visible)
                {
                    visibleCols.Add(col);
                    var headerCell = worksheet.Cell(startRow, colIndex);
                    headerCell.Value = col.HeaderText;
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Font.FontSize = 10.5;
                    headerCell.Style.Font.FontColor = XLColor.White;
                    headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    colIndex++;
                }
            }

            // 3. Viết Dữ Liệu
            int currentRow = startRow + 1;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                colIndex = 1;
                foreach (var col in visibleCols)
                {
                    var cell = worksheet.Cell(currentRow, colIndex);
                    var val = row.Cells[col.Index].Value;

                    if (val != null)
                    {
                        string strVal = val.ToString() ?? "";
                        // Kiểm tra nếu là số hoặc tiền tệ
                        if (decimal.TryParse(strVal.Replace("đ", "").Replace(",", "").Trim(), out decimal decVal) 
                            && (strVal.Contains("đ") || strVal.Contains(",") || strVal.All(char.IsDigit)))
                        {
                            cell.Value = decVal;
                            if (strVal.Contains("đ"))
                            {
                                cell.Style.NumberFormat.Format = "#,##0 \"đ\"";
                            }
                            else if (strVal.Contains("%"))
                            {
                                cell.Style.NumberFormat.Format = "0.00\"%\"";
                            }
                            else
                            {
                                cell.Style.NumberFormat.Format = "#,##0";
                            }
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else
                        {
                            cell.Value = strVal;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        }
                    }

                    // Kẻ viền mỏng và màu xen kẽ
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");

                    if ((currentRow - startRow) % 2 == 0)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    }

                    colIndex++;
                }
                currentRow++;
            }

            // Kẻ viền cho hàng Header
            var headerRange = worksheet.Range(startRow, 1, startRow, visibleCols.Count);
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            headerRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1E3A8A");

            // Tự động căn chỉnh độ rộng cột
            worksheet.Columns(1, visibleCols.Count).AdjustToContents(12.0, 45.0);

            // Lưu file
            workbook.SaveAs(sfd.FileName);

            var result = MessageBox.Show(
                $"Xuất file Excel thành công!\nĐường dẫn: {sfd.FileName}\n\nBạn có muốn mở file ngay không?",
                "Xuất Excel Thành Công",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes && File.Exists(sfd.FileName))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = sfd.FileName,
                    UseShellExecute = true
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất file Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
