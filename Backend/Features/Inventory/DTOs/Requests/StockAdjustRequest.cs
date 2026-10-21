namespace SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;

public class StockAdjustRequest
{
    public int QuantityChange { get; set; }
    public string Note { get; set; } = string.Empty;
}
