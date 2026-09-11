using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Import.DTOs;

public class CreateImportReceiptRequest
{
    public int SupplierId { get; set; }
    public int BranchId { get; set; }
    public string? Note { get; set; }
}

public class AddImportDetailRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
