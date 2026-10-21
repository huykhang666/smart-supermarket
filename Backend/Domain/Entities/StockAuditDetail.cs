using System;

namespace SmartSupermarket.Backend.Domain.Entities;

public class StockAuditDetail
{
    public Guid Id { get; set; }
    public Guid StockAuditId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    
    public int SystemQty { get; set; }
    public int ActualQty { get; set; }
    public int VarianceQty => ActualQty - SystemQty;

    // Phân loại nguyên nhân hao hụt & Thống kê tài chính
    public string? LossReason { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VarianceAmount => VarianceQty * UnitPrice;
    
    public string Notes { get; set; } = string.Empty;

    public StockAudit? StockAudit { get; set; }
}
