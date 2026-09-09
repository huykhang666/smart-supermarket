using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Products.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _dbContext;

    public SupplierRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.SupplierId == id, cancellationToken);
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .OrderBy(s => s.SupplierName)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Supplier> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(s => s.SupplierName.ToLower().Contains(searchLower) ||
                                     (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(searchLower)) ||
                                     (s.PhoneNumber != null && s.PhoneNumber.Contains(searchLower)) ||
                                     (s.Email != null && s.Email.ToLower().Contains(searchLower)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.SupplierName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsPhoneOrEmailAsync(string? phoneNumber, string? email, int? excludeSupplierId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var query = _dbContext.Suppliers.AsQueryable();

        if (excludeSupplierId.HasValue)
        {
            query = query.Where(s => s.SupplierId != excludeSupplierId.Value);
        }

        var phoneTrim = phoneNumber?.Trim();
        var emailTrim = email?.Trim().ToLower();

        return await query.AnyAsync(s =>
            (!string.IsNullOrEmpty(phoneTrim) && s.PhoneNumber != null && s.PhoneNumber == phoneTrim) ||
            (!string.IsNullOrEmpty(emailTrim) && s.Email != null && s.Email.ToLower() == emailTrim),
            cancellationToken);
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await _dbContext.Suppliers.AddAsync(supplier, cancellationToken);
    }

    public void Update(Supplier supplier)
    {
        _dbContext.Suppliers.Update(supplier);
    }

    public void Delete(Supplier supplier)
    {
        _dbContext.Suppliers.Remove(supplier);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
