using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Repositories.Implementations;

public class AiRecipeRepository : IAiRecipeRepository
{
    private readonly AppDbContext _context;

    public AiRecipeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetRecipeIngredientsAsync(
        string dishName,
        int servings,
        CancellationToken cancellationToken = default)
    {
        var lowerDish = dishName?.ToLowerInvariant() ?? string.Empty;
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active);

        if (lowerDish.Contains("lẩu bò"))
        {
            return await query.Where(p => p.ProductName.Contains("Bò") || p.ProductName.Contains("Nấm") || p.ProductName.Contains("Cải") || p.ProductName.Contains("Lẩu"))
                .Take(6)
                .ToListAsync(cancellationToken);
        }

        if (lowerDish.Contains("lẩu thái") || lowerDish.Contains("hải sản"))
        {
            return await query.Where(p => p.ProductName.Contains("Tôm") || p.ProductName.Contains("Mực") || p.ProductName.Contains("Cá") || p.ProductName.Contains("Lẩu"))
                .Take(6)
                .ToListAsync(cancellationToken);
        }

        if (lowerDish.Contains("thịt kho") || lowerDish.Contains("kho tàu"))
        {
            return await query.Where(p => p.ProductName.Contains("Thịt") || p.ProductName.Contains("Trứng") || p.ProductName.Contains("Muống") || p.ProductName.Contains("Nước mắm"))
                .Take(4)
                .ToListAsync(cancellationToken);
        }

        return await query.Where(p => p.ProductName.Contains("Trứng") || p.ProductName.Contains("Rau") || p.ProductName.Contains("Dầu"))
            .Take(3)
            .ToListAsync(cancellationToken);
    }

    public Task<string?> GetCookingTipsAsync(string dishName, CancellationToken cancellationToken = default)
    {
        var lower = dishName?.ToLowerInvariant() ?? string.Empty;

        if (lower.Contains("rau muống"))
        {
            return Task.FromResult<string?>("Luộc rau với nước sôi bùng ngập rau, thêm 1/2 thìa muối hạt và không đậy nắp vung. Vớt ra thả vào âu nước đá để rau giòn xanh mướt.");
        }

        if (lower.Contains("bò"))
        {
            return Task.FromResult<string?>("Thái thịt bò mỏng ngang thớ. Ướp dầu ăn và bột bắp 15 phút để khóa ẩm. Xào lửa cực lớn đảo nhanh tay trong 1-2 phút.");
        }

        return Task.FromResult<string?>("Nêm nếm gia vị vừa miệng, ướp thịt cá trước 15-20 phút để món ăn đậm đà chuẩn vị gia đình.");
    }
}
