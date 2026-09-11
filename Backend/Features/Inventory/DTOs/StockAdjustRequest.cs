namespace SmartSupermarket.Backend.Features.Inventory.DTOs;

public class StockAdjustRequest
{
    public int QuantityChange { get; set; }
    public string Note { get; set; } = string.Empty;
}
