namespace SmartSupermarket.Backend.Domain.Entities;

public class ProductUnitConversion
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UnitName { get; set; } = string.Empty; // E.g. "Lốc 6", "Thùng 24"
    public int ConversionRate { get; set; } = 1; // E.g. 6, 24
    public decimal Price { get; set; } // Price for this unit
    public string? Barcode { get; set; } // Optional distinct barcode for pack/box
    public bool IsDefault { get; set; } = false;

    public Product? Product { get; set; }
}
