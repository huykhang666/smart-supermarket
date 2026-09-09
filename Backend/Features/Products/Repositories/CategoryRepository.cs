using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Products.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // CRUD
    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Include(c => c.Products)
            .Include(c => c.ProductCategories)
            .FirstOrDefaultAsync(c => c.CategoryId == id, cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.CategoryName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _dbContext.Categories.AddAsync(category, cancellationToken);
    }

    public void Update(Category category)
    {
        _dbContext.Categories.Update(category);
    }

    public void Delete(Category category)
    {
        _dbContext.Categories.Remove(category);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Search
    public async Task<IEnumerable<Category>> SearchAsync(string search, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return await GetAllAsync(cancellationToken);
        }

        var searchLower = search.Trim().ToLower();
        return await _dbContext.Categories
            .Where(c => c.CategoryName.ToLower().Contains(searchLower) ||
                        (c.Description != null && c.Description.ToLower().Contains(searchLower)))
            .OrderBy(c => c.CategoryName)
            .ToListAsync(cancellationToken);
    }

    // Tree Query
    public async Task<IEnumerable<Category>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.CategoryName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    // Pagination
    public async Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(c => c.CategoryName.ToLower().Contains(searchLower) ||
                                     (c.Description != null && c.Description.ToLower().Contains(searchLower)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.CategoryName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    // Get Dropdown
    public async Task<IEnumerable<Category>> GetDropdownAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Select(c => new Category
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            })
            .OrderBy(c => c.CategoryName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    // Get Products By Category
    public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Where(p => p.CategoryId == categoryId || p.ProductCategories.Any(pc => pc.CategoryId == categoryId))
            .Include(p => p.Supplier)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    // Existence Check
    public async Task<bool> ExistsNameAsync(string categoryName, int? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsQueryable();

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId != excludeCategoryId.Value);
        }

        var nameTrim = categoryName.Trim().ToLower();
        return await query.AnyAsync(c => c.CategoryName.ToLower() == nameTrim, cancellationToken);
    }
}
