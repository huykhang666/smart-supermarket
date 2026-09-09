using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Suppliers.Repositories;

public class ProductSupplierRepository : IProductSupplierRepository
{
    private readonly AppDbContext _dbContext;

    public ProductSupplierRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductSupplier?> GetLinkAsync(int productId, int supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductSuppliers
            .Include(ps => ps.Product)
            .Include(ps => ps.Supplier)
            .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.SupplierId == supplierId, cancellationToken);
    }

    public async Task<IEnumerable<ProductSupplier>> GetSuppliersByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductSuppliers
            .Where(ps => ps.ProductId == productId)
            .Include(ps => ps.Supplier)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductSupplier>> GetSuppliedProductsBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId)
            .Include(ps => ps.Product)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddLinkAsync(ProductSupplier productSupplier, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProductSuppliers.AddAsync(productSupplier, cancellationToken);
    }

    public void UpdateLink(ProductSupplier productSupplier)
    {
        _dbContext.ProductSuppliers.Update(productSupplier);
    }

    public void Unlink(ProductSupplier productSupplier)
    {
        _dbContext.ProductSuppliers.Remove(productSupplier);
    }

    public async Task ResetOtherDefaultsAsync(int productId, int currentSupplierId, CancellationToken cancellationToken = default)
    {
        var otherLinks = await _dbContext.ProductSuppliers
            .Where(ps => ps.ProductId == productId && ps.SupplierId != currentSupplierId && ps.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var link in otherLinks)
        {
            link.IsDefault = false;
            link.UpdatedAt = DateTime.UtcNow;
            _dbContext.ProductSuppliers.Update(link);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
