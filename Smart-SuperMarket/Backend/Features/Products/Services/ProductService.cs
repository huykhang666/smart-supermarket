using SmartSupermarket.Backend.Features.Products.DTOs;

namespace SmartSupermarket.Backend.Features.Products.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByBarcodeAsync(string barcode);
}

public class ProductService : IProductService
{
    public Task<IEnumerable<ProductDto>> GetAllAsync() => throw new NotImplementedException();
    public Task<ProductDto?> GetByBarcodeAsync(string barcode) => throw new NotImplementedException();
}
