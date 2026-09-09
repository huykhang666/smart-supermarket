using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Products.DTOs;

public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public string ImageUrl { get; set; } = "/images/products/no-image.png";
    public string Unit { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }
    public string StatusName => Status == ProductStatus.Active ? "Đang bán" : "Ngừng kinh doanh";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Calculated Financial Fields
    public decimal GrossProfit => CostPrice.HasValue ? (Price - CostPrice.Value) : Price;
    public decimal ProfitMarginPercentage => (Price > 0 && CostPrice.HasValue) 
        ? Math.Round(((Price - CostPrice.Value) / Price) * 100, 2) 
        : 0;
    public bool IsNegativeMarginWarning => CostPrice.HasValue && Price < CostPrice.Value;
}

public class ProductBarcodeDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CreateProductRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class UpdateProductRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class UpdatePriceRequest
{
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
}

public class ProductPriceHistoryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal? CurrentCostPrice { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMarginPercentage { get; set; }
    public bool IsNegativeMarginWarning { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class ProductPagedResult
{
    public IEnumerable<ProductDto> Items { get; set; } = new List<ProductDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
