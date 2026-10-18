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

    /// <summary>
    /// Xuất Biên Bản Đối Soát Kiểm Kê & Hao Hụt Tài Chính chuyên nghiệp chuẩn mẫu siêu thị.
    /// Bao gồm đầy đủ thông tin đợt kiểm, bảng KPI đối soát, phân loại 4 nguyên nhân hao hụt và khối chữ ký 3 bên.
    /// </summary>
    public static bool ExportStockAuditReportToExcel(StockAuditDTO audit)
    {
        if (audit.Details == null || audit.Details.Count == 0)
        {
            MessageBox.Show("Đợt kiểm kê chưa có dữ liệu sản phẩm để xuất biên bản!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        string auditCode = audit.Id.ToString()[..8].ToUpper();
        using var sfd = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"BienBanKiemKe_{auditCode}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            Title = $"Lưu Biên Bản Kiểm Kê #{auditCode}"
        };

        if (sfd.ShowDialog() != DialogResult.OK)
            return false;

        try
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BienBanKiemKe");

            // 1. HEADER DOANH NGHIỆP
            ws.Cell(1, 1).Value = "SMART SUPERMARKET - HỆ THỐNG QUẢN LÝ SIÊU THỊ THÔNG MINH";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 10;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");

            ws.Cell(2, 1).Value = "BIÊN BẢN ĐỐI SOÁT KIỂM KÊ KHO & HAO HỤT TÀI CHÍNH";
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 16;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

            string statusText = audit.Status switch
            {
                "Reconciled" => "🟢 ĐÃ CÂN BẰNG KHO",
                "Completed" => "🟠 CHỜ DUYỆT CÂN BẰNG",
                _ => "🟡 ĐANG KIỂM ĐẾM"
            };

            ws.Cell(3, 1).Value = $"Mã đợt kiểm: #{auditCode}  |  Ngày kiểm: {audit.AuditDate:dd/MM/yyyy HH:mm}  |  Khu vực: {audit.Zone}  |  Trạng thái: {statusText}";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontSize = 10;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#334155");

            string reconcileInfo = audit.ReconciledAt.HasValue
                ? $"Người cân bằng: {audit.ReconciledBy}  |  Thời gian cân bằng: {audit.ReconciledAt.Value:dd/MM/yyyy HH:mm}"
                : "Chưa cân bằng số tồn hệ thống";
            ws.Cell(4, 1).Value = $"Nhân viên kiểm đếm: {audit.CreatedBy}  |  {reconcileInfo}  |  Ghi chú: {audit.Notes}";
            ws.Cell(4, 1).Style.Font.FontSize = 9.5;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");

            // 2. KHỐI TÓM TẮT KPI TÀI CHÍNH
            int matchCount = audit.Details.Count(d => d.VarianceQty == 0);
            int diffCount = audit.Details.Count(d => d.VarianceQty != 0);

            ws.Cell(6, 1).Value = "TỔNG HỢP KẾT QUẢ ĐỐI SOÁT";
            ws.Cell(6, 1).Style.Font.Bold = true;
            ws.Cell(6, 1).Style.Font.FontSize = 11;
            ws.Cell(6, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

            // KPI Box
            ws.Cell(7, 1).Value = "Tổng SKU kiểm:";
            ws.Cell(7, 2).Value = audit.TotalItems;
            ws.Cell(7, 3).Value = "SKU khớp tồn:";
            ws.Cell(7, 4).Value = matchCount;
            ws.Cell(7, 5).Value = "SKU chênh lệch:";
            ws.Cell(7, 6).Value = diffCount;
            ws.Cell(7, 7).Value = "TỔNG TIỀN HAO HỤT:";
            ws.Cell(7, 8).Value = audit.TotalLossAmount;
            ws.Cell(7, 8).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(7, 8).Style.Font.Bold = true;
            ws.Cell(7, 8).Style.Font.FontColor = XLColor.FromHtml("#DC2626");

            var kpiRange = ws.Range(7, 1, 7, 8);
            kpiRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            kpiRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            kpiRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");

            // 3. TIÊU ĐỀ BẢNG CHI TIẾT
            int startRow = 9;
            string[] headers = new[]
            {
                "STT", "Mã Barcode", "Tên Sản Phẩm", "Đơn Giá Vốn", 
                "Tồn Sổ Sách", "Thực Tế Đếm", "Chênh Lệch (SL)", "Thành Tiền Lệch", 
                "Trạng Thái", "Nguyên Nhân Hao Hụt", "Ghi Chú"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var hCell = ws.Cell(startRow, i + 1);
                hCell.Value = headers[i];
                hCell.Style.Font.Bold = true;
                hCell.Style.Font.FontSize = 10;
                hCell.Style.Font.FontColor = XLColor.White;
                hCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                hCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                hCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                hCell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#0F172A");
            }
            ws.Row(startRow).Height = 28;

            // 4. DỮ LIỆU SẢN PHẨM
            int curRow = startRow + 1;
            int stt = 1;
            decimal totalVarianceAmount = 0;
            int totalVarianceQty = 0;

            foreach (var item in audit.Details)
            {
                string statusItem = item.VarianceQty == 0 ? "Khớp" : (item.VarianceQty < 0 ? "Thiếu" : "Thừa");
                string lossReason = !string.IsNullOrWhiteSpace(item.LossReason) ? item.LossReason : (item.VarianceQty == 0 ? "Khớp tồn" : "Chưa chọn");

                totalVarianceQty += item.VarianceQty;
                totalVarianceAmount += item.VarianceAmount;

                ws.Cell(curRow, 1).Value = stt++;
                ws.Cell(curRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(curRow, 2).Value = item.Barcode;
                ws.Cell(curRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(curRow, 3).Value = item.ProductName;

                ws.Cell(curRow, 4).Value = item.UnitPrice;
                ws.Cell(curRow, 4).Style.NumberFormat.Format = "#,##0 \"đ\"";
                ws.Cell(curRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(curRow, 5).Value = item.SystemQty;
                ws.Cell(curRow, 5).Style.NumberFormat.Format = "#,##0";
                ws.Cell(curRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(curRow, 6).Value = item.ActualQty;
                ws.Cell(curRow, 6).Style.NumberFormat.Format = "#,##0";
                ws.Cell(curRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(curRow, 7).Value = item.VarianceQty;
                ws.Cell(curRow, 7).Style.NumberFormat.Format = "+#,##0;-#,##0;0";
                ws.Cell(curRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(curRow, 8).Value = item.VarianceAmount;
                ws.Cell(curRow, 8).Style.NumberFormat.Format = "#,##0 \"đ\"";
                ws.Cell(curRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(curRow, 9).Value = statusItem;
                ws.Cell(curRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(curRow, 10).Value = lossReason;
                ws.Cell(curRow, 11).Value = item.Notes;

                // Tô màu theo mức chênh lệch
                var rowRange = ws.Range(curRow, 1, curRow, headers.Length);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");

                if (item.VarianceQty < 0)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2");
                    ws.Cell(curRow, 7).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                    ws.Cell(curRow, 7).Style.Font.Bold = true;
                    ws.Cell(curRow, 8).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                    ws.Cell(curRow, 9).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                }
                else if (item.VarianceQty > 0)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFBEB");
                    ws.Cell(curRow, 7).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                    ws.Cell(curRow, 7).Style.Font.Bold = true;
                    ws.Cell(curRow, 8).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                    ws.Cell(curRow, 9).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                }
                else
                {
                    if (stt % 2 == 0) rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                curRow++;
            }

            // 5. HÀNG TỔNG CỘNG (TOTAL FOOTER ROW)
            ws.Cell(curRow, 3).Value = "TỔNG CỘNG";
            ws.Cell(curRow, 3).Style.Font.Bold = true;
            ws.Cell(curRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(curRow, 7).Value = totalVarianceQty;
            ws.Cell(curRow, 7).Style.NumberFormat.Format = "+#,##0;-#,##0;0";
            ws.Cell(curRow, 7).Style.Font.Bold = true;
            ws.Cell(curRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(curRow, 8).Value = totalVarianceAmount;
            ws.Cell(curRow, 8).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(curRow, 8).Style.Font.Bold = true;
            ws.Cell(curRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var totalRange = ws.Range(curRow, 1, curRow, headers.Length);
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            totalRange.Style.Font.Bold = true;
            curRow += 3;

            // 6. KHỐI CHỮ KÝ 3 BÊN (SIGNATURE BLOCK)
            ws.Cell(curRow, 2).Value = "NGƯỜI LẬP BIÊN BẢN / KIỂM ĐẾM";
            ws.Cell(curRow, 2).Style.Font.Bold = true;
            ws.Cell(curRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(curRow, 5).Value = "THỦ KHO PHỤ TRÁCH";
            ws.Cell(curRow, 5).Style.Font.Bold = true;
            ws.Cell(curRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(curRow, 9).Value = "CỬA HÀNG TRƯỞNG / KẾ TOÁN TRƯỞNG";
            ws.Cell(curRow, 9).Style.Font.Bold = true;
            ws.Cell(curRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(curRow + 1, 2).Value = "(Ký và ghi rõ họ tên)";
            ws.Cell(curRow + 1, 2).Style.Font.Italic = true;
            ws.Cell(curRow + 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(curRow + 1, 5).Value = "(Ký và ghi rõ họ tên)";
            ws.Cell(curRow + 1, 5).Style.Font.Italic = true;
            ws.Cell(curRow + 1, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(curRow + 1, 9).Value = "(Ký duyệt & đóng dấu)";
            ws.Cell(curRow + 1, 9).Style.Font.Italic = true;
            ws.Cell(curRow + 1, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(curRow + 5, 2).Value = audit.CreatedBy;
            ws.Cell(curRow + 5, 2).Style.Font.Bold = true;
            ws.Cell(curRow + 5, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(curRow + 5, 9).Value = !string.IsNullOrWhiteSpace(audit.ReconciledBy) ? audit.ReconciledBy : "Cửa hàng trưởng";
            ws.Cell(curRow + 5, 9).Style.Font.Bold = true;
            ws.Cell(curRow + 5, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Tự động căn chỉnh độ rộng cột
            ws.Columns(1, headers.Length).AdjustToContents(10.0, 40.0);

            workbook.SaveAs(sfd.FileName);

            var result = MessageBox.Show(
                $"Xuất Biên bản kiểm kê Excel thành công!\nĐường dẫn: {sfd.FileName}\n\nBạn có muốn mở file ngay không?",
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
            MessageBox.Show($"Lỗi khi xuất Biên bản kiểm kê Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
