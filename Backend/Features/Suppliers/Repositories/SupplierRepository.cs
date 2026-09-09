using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Suppliers.Repositories;

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
            .Include(s => s.ProductSuppliers)
                .ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(s => s.SupplierId == id, cancellationToken);
    }

    public async Task<Supplier?> GetByCodeAsync(string supplierCode, CancellationToken cancellationToken = default)
    {
        var codeTrim = supplierCode.Trim().ToLower();
        return await _dbContext.Suppliers
            .Include(s => s.Products)
            .Include(s => s.ProductSuppliers)
                .ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(s => s.SupplierCode.ToLower() == codeTrim, cancellationToken);
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Suppliers
            .Include(s => s.Products)
            .Include(s => s.ProductSuppliers)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.Status == 1);
        }

        return await query
            .OrderBy(s => s.SupplierName)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Supplier> Items, int TotalCount)> GetPagedAsync(
        string? search,
        byte? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Suppliers
            .Include(s => s.Products)
            .Include(s => s.ProductSuppliers)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchNormalized = RemoveAccents(search.Trim().ToLower());
            var suppliers = await query.ToListAsync(cancellationToken);
            var filtered = suppliers.Where(s =>
                RemoveAccents(s.SupplierName.ToLower()).Contains(searchNormalized) ||
                RemoveAccents(s.SupplierCode.ToLower()).Contains(searchNormalized) ||
                (!string.IsNullOrWhiteSpace(s.ContactPerson) && RemoveAccents(s.ContactPerson.ToLower()).Contains(searchNormalized)) ||
                (!string.IsNullOrWhiteSpace(s.PhoneNumber) && s.PhoneNumber.Contains(searchNormalized)) ||
                (!string.IsNullOrWhiteSpace(s.Email) && s.Email.ToLower().Contains(searchNormalized))
            ).ToList();

            int total = filtered.Count;
            var pagedItems = filtered
                .OrderBy(s => s.SupplierName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedItems, total);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.SupplierName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Supplier>> SearchAsync(string search, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var allSuppliers = await GetAllAsync(includeInactive, cancellationToken);
        if (string.IsNullOrWhiteSpace(search))
        {
            return allSuppliers;
        }

        var searchNormalized = RemoveAccents(search.Trim().ToLower());
        return allSuppliers.Where(s =>
            RemoveAccents(s.SupplierName.ToLower()).Contains(searchNormalized) ||
            RemoveAccents(s.SupplierCode.ToLower()).Contains(searchNormalized) ||
            (!string.IsNullOrWhiteSpace(s.ContactPerson) && RemoveAccents(s.ContactPerson.ToLower()).Contains(searchNormalized)) ||
            (!string.IsNullOrWhiteSpace(s.PhoneNumber) && s.PhoneNumber.Contains(searchNormalized)) ||
            (!string.IsNullOrWhiteSpace(s.Email) && s.Email.ToLower().Contains(searchNormalized))
        )
        .OrderBy(s => s.SupplierName);
    }

    public async Task<IEnumerable<Supplier>> GetDropdownAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .Where(s => s.Status == 1)
            .OrderBy(s => s.SupplierName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsNameAsync(string supplierName, int? excludeSupplierId = null, CancellationToken cancellationToken = default)
    {
        var nameTrim = supplierName.Trim().ToLower();
        var query = _dbContext.Suppliers.AsQueryable();

        if (excludeSupplierId.HasValue)
        {
            query = query.Where(s => s.SupplierId != excludeSupplierId.Value);
        }

        return await query.AnyAsync(s => s.SupplierName.ToLower() == nameTrim, cancellationToken);
    }

    public async Task<bool> ExistsCodeAsync(string supplierCode, int? excludeSupplierId = null, CancellationToken cancellationToken = default)
    {
        var codeTrim = supplierCode.Trim().ToLower();
        var query = _dbContext.Suppliers.AsQueryable();

        if (excludeSupplierId.HasValue)
        {
            query = query.Where(s => s.SupplierId != excludeSupplierId.Value);
        }

        return await query.AnyAsync(s => s.SupplierCode.ToLower() == codeTrim, cancellationToken);
    }

    public async Task<IEnumerable<ProductSupplier>> GetSuppliedProductsBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId)
            .Include(ps => ps.Product)
            .Include(ps => ps.Supplier)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
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

    private static string RemoveAccents(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }
}
