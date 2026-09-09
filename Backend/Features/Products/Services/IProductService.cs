using SmartSupermarket.Backend.Features.Products.DTOs;

namespace SmartSupermarket.Backend.Features.Products.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductPagedResult> SearchProductsAsync(
        string? search,
        int? categoryId,
        int? supplierId,
        byte? status,
        decimal? minPrice,
        decimal? maxPrice,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ProductBarcodeDto?> GetBarcodeInfoAsync(string barcode, CancellationToken cancellationToken = default);
    Task<string> GenerateBarcodeAsync(int? productId = null, CancellationToken cancellationToken = default);
    Task<string> GenerateInternalProductCodeAsync(CancellationToken cancellationToken = default);
    Task<string> UploadProductImageAsync(int productId, Stream imageStream, string fileName, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductPriceAsync(int productId, UpdatePriceRequest request, CancellationToken cancellationToken = default);
    Task<ProductPriceHistoryDto?> GetProductPriceHistoryAsync(int productId, CancellationToken cancellationToken = default);
}
