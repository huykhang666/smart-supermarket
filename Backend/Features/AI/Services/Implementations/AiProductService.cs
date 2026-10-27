using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.Repositories;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Services.Implementations;

public class AiProductService : IAiProductService
{
    private readonly IAiProductRepository _productRepository;

    public AiProductService(IAiProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<Product>> SearchProductsAsync(string keyword, int? categoryId = null, int limit = 15, CancellationToken cancellationToken = default)
    {
        return await _productRepository.SearchProductsAsync(keyword, categoryId, limit, cancellationToken);
    }

    public async Task<Product?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _productRepository.GetProductByIdAsync(productId, cancellationToken);
    }

    public async Task<Product?> GetAlternativeProductAsync(int originalProductId, decimal maxPrice, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetProductByIdAsync(originalProductId, cancellationToken);
        if (product == null) return null;

        return await _productRepository.GetAlternativeProductAsync(originalProductId, product.CategoryId, maxPrice, cancellationToken);
    }
}
