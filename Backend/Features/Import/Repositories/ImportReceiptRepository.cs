using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Import.Repositories;

public class ImportReceiptRepository : IImportReceiptRepository
{
    private readonly AppDbContext _dbContext;

    public ImportReceiptRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IEnumerable<ImportReceipt> Items, int TotalCount)> GetAllAsync(int? branchId, int? supplierId, ImportStatus? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _dbContext.ImportReceipts
            .Include(r => r.Supplier)
            .Include(r => r.ImportedByUser)
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(r => r.BranchId == branchId.Value);

        if (supplierId.HasValue)
            query = query.Where(r => r.SupplierId == supplierId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(r => r.ImportDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ImportDate <= toDate.Value);

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.ImportDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ImportReceipt?> GetByIdWithDetailsAsync(int importReceiptId)
    {
        return await _dbContext.ImportReceipts
            .Include(r => r.Supplier)
            .Include(r => r.ImportedByUser)
            .Include(r => r.ConfirmedByUser)
            .Include(r => r.ImportDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(r => r.ImportReceiptId == importReceiptId);
    }

    public async Task CreateAsync(ImportReceipt importReceipt)
    {
        await _dbContext.ImportReceipts.AddAsync(importReceipt);
    }

    public async Task UpdateStatusAsync(int importReceiptId, ImportStatus status, DateTime? confirmedAt, int? confirmedByUserId)
    {
        var receipt = await _dbContext.ImportReceipts.FindAsync(importReceiptId);
        if (receipt != null)
        {
            receipt.Status = status;
            receipt.ConfirmedAt = confirmedAt;
            receipt.ConfirmedByUserId = confirmedByUserId;
            _dbContext.ImportReceipts.Update(receipt);
        }
    }

    public async Task UpdateTotalAmountAsync(int importReceiptId, decimal totalAmount)
    {
        var receipt = await _dbContext.ImportReceipts.FindAsync(importReceiptId);
        if (receipt != null)
        {
            receipt.TotalAmount = totalAmount;
            _dbContext.ImportReceipts.Update(receipt);
        }
    }

    public async Task<int> CountTodayAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        return await _dbContext.ImportReceipts
            .Where(r => r.ImportDate >= today && r.ImportDate < tomorrow)
            .CountAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
