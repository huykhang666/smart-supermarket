namespace SmartSupermarket.Backend.Features.Products.DTOs;

public class ProductUnitConversionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int ConversionRate { get; set; }
    public decimal Price { get; set; }
    public string? Barcode { get; set; }
    public bool IsDefault { get; set; }
}

public class CreateProductUnitRequest
{
    public string UnitName { get; set; } = string.Empty;
    public int ConversionRate { get; set; } = 1;
    public decimal? Price { get; set; }
    public string? Barcode { get; set; }
    public bool IsDefault { get; set; } = false;
}
