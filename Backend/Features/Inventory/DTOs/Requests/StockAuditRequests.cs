namespace SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;

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
