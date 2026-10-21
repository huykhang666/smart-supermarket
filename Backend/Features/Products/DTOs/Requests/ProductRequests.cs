using System;

namespace SmartSupermarket.Backend.Features.Products.DTOs.Requests;

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
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class UpdatePriceRequest
{
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
}
