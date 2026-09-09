using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Products.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int? categoryId,
        int? supplierId,
        byte? status,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsBarcodeAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    void Delete(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
