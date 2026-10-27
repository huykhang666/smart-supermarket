using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public class ProductTool : IProductTool
{
    private readonly IAiProductService _productService;

    public ProductTool(IAiProductService productService)
    {
        _productService = productService;
    }

    public Task<List<Product>> SearchAsync(string keyword, int? categoryId = null, int limit = 15, CancellationToken cancellationToken = default)
    {
        return _productService.SearchProductsAsync(keyword, categoryId, limit, cancellationToken);
    }

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _productService.GetProductByIdAsync(productId, cancellationToken);
    }

    public Task<Product?> GetAlternativeAsync(int originalProductId, decimal maxPrice, CancellationToken cancellationToken = default)
    {
        return _productService.GetAlternativeProductAsync(originalProductId, maxPrice, cancellationToken);
    }
}
