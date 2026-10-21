namespace SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;

public class ShelfLifeScanRequest
{
    public bool IsEveningFlashSale { get; set; } = false;
    public int BranchId { get; set; } = 1;
    public bool DryRun { get; set; } = false;
}
