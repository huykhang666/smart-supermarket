using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Domain.Entities;

public class StockAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime AuditDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Completed
    public string Notes { get; set; } = string.Empty;
    
    public ICollection<StockAuditDetail> Details { get; set; } = new List<StockAuditDetail>();
}
