using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Import.Services;

public interface IImportReceiptService
{
    Task<(IEnumerable<ImportReceipt> Items, int TotalCount)> GetImportReceiptsAsync(int? branchId, int? supplierId, ImportStatus? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<ImportReceipt?> GetImportReceiptByIdAsync(int importReceiptId);
    Task<ImportReceipt> CreateImportReceiptAsync(int supplierId, int branchId, string? note, int userId);
    Task<ImportDetail> AddImportDetailAsync(int importReceiptId, int productId, int quantity, decimal costPrice, DateOnly? expiryDate, int userId);
    Task ConfirmImportReceiptAsync(int importReceiptId, int userId);
    Task CancelImportReceiptAsync(int importReceiptId, int userId);
}
