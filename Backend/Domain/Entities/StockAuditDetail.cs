using System;

namespace SmartSupermarket.Backend.Domain.Entities;

public class StockAuditDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StockAuditId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    
    public int SystemQty { get; set; }
    public int ActualQty { get; set; }
    public int VarianceQty => ActualQty - SystemQty;
    
    public string Notes { get; set; } = string.Empty;

    public StockAudit? StockAudit { get; set; }
}
