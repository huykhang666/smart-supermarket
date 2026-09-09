using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Categories.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Include(c => c.Parent)
            .Include(c => c.SubCategories)
            .Include(c => c.Products)
            .Include(c => c.ProductCategories)
            .FirstOrDefaultAsync(c => c.CategoryId == id, cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugTrim = slug.Trim().ToLower();
        return await _dbContext.Categories
            .Include(c => c.Parent)
            .Include(c => c.SubCategories)
            .Include(c => c.Products)
            .Include(c => c.ProductCategories)
            .FirstOrDefaultAsync(c => c.Slug.ToLower() == slugTrim, cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories
            .Include(c => c.Parent)
            .Include(c => c.Products)
            .Include(c => c.ProductCategories)
            .Include(c => c.SubCategories)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.Status == 1);
        }

        return await query
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.CategoryName)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(
        string? search,
        byte? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories
            .Include(c => c.Parent)
            .Include(c => c.Products)
            .Include(c => c.ProductCategories)
            .Include(c => c.SubCategories)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchNormalized = RemoveAccents(search.Trim().ToLower());
            var categories = await query.ToListAsync(cancellationToken);
            var filtered = categories.Where(c => 
                RemoveAccents(c.CategoryName.ToLower()).Contains(searchNormalized) ||
                RemoveAccents(c.Slug.ToLower()).Contains(searchNormalized) ||
                (!string.IsNullOrWhiteSpace(c.Description) && RemoveAccents(c.Description.ToLower()).Contains(searchNormalized))
            ).ToList();

            int total = filtered.Count;
            var pagedItems = filtered
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.CategoryName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedItems, total);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.CategoryName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Category>> SearchAsync(string search, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return await GetAllAsync(includeInactive, cancellationToken);
        }

        var allCategories = await GetAllAsync(includeInactive, cancellationToken);
        var searchNormalized = RemoveAccents(search.Trim().ToLower());

        return allCategories.Where(c =>
            RemoveAccents(c.CategoryName.ToLower()).Contains(searchNormalized) ||
            RemoveAccents(c.Slug.ToLower()).Contains(searchNormalized) ||
            (!string.IsNullOrWhiteSpace(c.Description) && RemoveAccents(c.Description.ToLower()).Contains(searchNormalized))
        )
        .OrderBy(c => c.OrderIndex)
        .ThenBy(c => c.CategoryName);
    }

    public async Task<IEnumerable<Category>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var allCategories = await _dbContext.Categories
            .Include(c => c.Products)
            .Include(c => c.ProductCategories)
            .Include(c => c.SubCategories)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.CategoryName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Build hierarchy: Top-level roots are ParentId == null
        var categoryMap = allCategories.ToDictionary(c => c.CategoryId);
        var roots = new List<Category>();

        foreach (var category in allCategories)
        {
            if (category.ParentId == null || !categoryMap.ContainsKey(category.ParentId.Value))
            {
                roots.Add(category);
            }
        }

        return roots;
    }

    public async Task<IEnumerable<Category>> GetDropdownAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Where(c => c.Status == 1)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.CategoryName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Where(p => p.CategoryId == categoryId || p.ProductCategories.Any(pc => pc.CategoryId == categoryId))
            .Include(p => p.Supplier)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsNameAsync(string categoryName, int? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var nameTrim = categoryName.Trim().ToLower();
        var query = _dbContext.Categories.AsQueryable();

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId != excludeCategoryId.Value);
        }

        return await query.AnyAsync(c => c.CategoryName.ToLower() == nameTrim, cancellationToken);
    }

    public async Task<bool> ExistsSlugAsync(string slug, int? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var slugTrim = slug.Trim().ToLower();
        var query = _dbContext.Categories.AsQueryable();

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId != excludeCategoryId.Value);
        }

        return await query.AnyAsync(c => c.Slug.ToLower() == slugTrim, cancellationToken);
    }

    public async Task<List<int>> GetDescendantCategoryIdsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var allCategories = await _dbContext.Categories
            .Select(c => new { c.CategoryId, c.ParentId })
            .ToListAsync(cancellationToken);

        var descendants = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(categoryId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = allCategories.Where(c => c.ParentId == currentId).Select(c => c.CategoryId);
            foreach (var childId in children)
            {
                descendants.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return descendants;
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

    public async Task MoveProductCategoryRecordsAsync(int sourceCategoryId, int targetCategoryId, CancellationToken cancellationToken = default)
    {
        // 1. Move direct products if Product.CategoryId == sourceCategoryId
        var directProducts = await _dbContext.Products
            .Where(p => p.CategoryId == sourceCategoryId)
            .ToListAsync(cancellationToken);

        foreach (var p in directProducts)
        {
            p.CategoryId = targetCategoryId;
            p.UpdatedAt = DateTime.UtcNow;
            _dbContext.Products.Update(p);
        }

        // 2. Move Many-to-Many join entities (ProductCategory)
        var sourceJoinRecords = await _dbContext.ProductCategories
            .Where(pc => pc.CategoryId == sourceCategoryId)
            .ToListAsync(cancellationToken);

        foreach (var joinRecord in sourceJoinRecords)
        {
            _dbContext.ProductCategories.Remove(joinRecord);

            bool targetExists = await _dbContext.ProductCategories
                .AnyAsync(pc => pc.ProductId == joinRecord.ProductId && pc.CategoryId == targetCategoryId, cancellationToken);

            if (!targetExists)
            {
                await _dbContext.ProductCategories.AddAsync(new ProductCategory
                {
                    ProductId = joinRecord.ProductId,
                    CategoryId = targetCategoryId
                }, cancellationToken);
            }
        }
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
