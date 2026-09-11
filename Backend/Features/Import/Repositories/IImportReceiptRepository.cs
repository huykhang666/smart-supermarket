using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Import.Repositories;

public interface IImportReceiptRepository
{
    Task<(IEnumerable<ImportReceipt> Items, int TotalCount)> GetAllAsync(int? branchId, int? supplierId, ImportStatus? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<ImportReceipt?> GetByIdWithDetailsAsync(int importReceiptId);
    Task CreateAsync(ImportReceipt importReceipt);
    Task UpdateStatusAsync(int importReceiptId, ImportStatus status, DateTime? confirmedAt, int? confirmedByUserId);
    Task UpdateTotalAmountAsync(int importReceiptId, decimal totalAmount);
    Task<int> CountTodayAsync();
    Task SaveChangesAsync();
}
