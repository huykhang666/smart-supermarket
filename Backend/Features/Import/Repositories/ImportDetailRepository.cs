using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Import.Repositories;

public class ImportDetailRepository : IImportDetailRepository
{
    private readonly AppDbContext _dbContext;

    public ImportDetailRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ImportDetail importDetail)
    {
        await _dbContext.ImportDetails.AddAsync(importDetail);
    }

    public async Task<IEnumerable<ImportDetail>> GetByReceiptIdAsync(int importReceiptId)
    {
        return await _dbContext.ImportDetails
            .Include(d => d.Product)
            .Where(d => d.ImportReceiptId == importReceiptId)
            .ToListAsync();
    }

    public async Task DeleteAsync(int importDetailId)
    {
        var detail = await _dbContext.ImportDetails.FindAsync(importDetailId);
        if (detail != null)
        {
            _dbContext.ImportDetails.Remove(detail);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
