using System;

namespace SmartSupermarket.Backend.Domain.Enums
{
    public enum ProductStatus : byte
    {
        Active = 1,
        Inactive = 2
    }
}

namespace SmartSupermarket.Backend.Features.Products.DTOs
{
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
        public Domain.Enums.ProductStatus Status { get; set; }
        public string StatusName => Status == Domain.Enums.ProductStatus.Active ? "Đang bán" : "Ngừng kinh doanh";
        public decimal? DiscountPrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int? Stock { get; set; }
        public string? StorageLocation { get; set; }
        public string? Description { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsNearExpiry => ExpiryDate.HasValue && (ExpiryDate.Value - DateTime.UtcNow).TotalDays <= 30;
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public decimal GrossProfit => CostPrice.HasValue ? (Price - CostPrice.Value) : Price;
        public decimal ProfitMarginPercentage => (Price > 0 && CostPrice.HasValue)
            ? Math.Round(((Price - CostPrice.Value) / Price) * 100, 2)
            : 0;
        public bool IsNegativeMarginWarning => CostPrice.HasValue && Price < CostPrice.Value;
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
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
