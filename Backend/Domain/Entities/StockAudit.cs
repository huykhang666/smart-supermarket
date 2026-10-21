using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Domain.Entities;

public class StockAudit
{
    public Guid Id { get; set; }
    public DateTime AuditDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    // Status: "Draft" (Đang kiểm đếm), "Completed" (Đã kiểm xong, chờ duyệt), "Reconciled" (Đã cân bằng kho), "Cancelled" (Đã hủy)
    public string Status { get; set; } = "Draft";
    public string Notes { get; set; } = string.Empty;

    // Dành cho luồng Cân Bằng Kho (Reconciliation)
    public DateTime? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }
    
    public ICollection<StockAuditDetail> Details { get; set; } = new List<StockAuditDetail>();
}
