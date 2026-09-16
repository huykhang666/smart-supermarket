using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.Inventory.DTOs;

public class StockAuditDTO
{
    public Guid Id { get; set; }
    public DateTime AuditDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<StockAuditDetailDTO> Details { get; set; } = new();
}

public class StockAuditDetailDTO
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int SystemQty { get; set; }
    public int ActualQty { get; set; }
    public int VarianceQty { get; set; }
}

public class CreateStockAuditRequest
{
    public string CreatedBy { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class SaveAuditDetailRequest
{
    public string Barcode { get; set; } = string.Empty;
    public int ActualQty { get; set; }
}
