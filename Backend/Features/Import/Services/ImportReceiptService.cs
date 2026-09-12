using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Import.Repositories;
using SmartSupermarket.Backend.Features.Inventory.Services;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Import.Services;

public class ImportReceiptService : IImportReceiptService
{
    private readonly IImportReceiptRepository _receiptRepository;
    private readonly IImportDetailRepository _detailRepository;
    private readonly IInventoryService _inventoryService;
    private readonly AppDbContext _dbContext;

    public ImportReceiptService(
        IImportReceiptRepository receiptRepository,
        IImportDetailRepository detailRepository,
        IInventoryService inventoryService,
        AppDbContext dbContext)
    {
        _receiptRepository = receiptRepository;
        _detailRepository = detailRepository;
        _inventoryService = inventoryService;
        _dbContext = dbContext;
    }

    public async Task<(IEnumerable<ImportReceipt> Items, int TotalCount)> GetImportReceiptsAsync(int? branchId, int? supplierId, ImportStatus? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        return await _receiptRepository.GetAllAsync(branchId, supplierId, status, fromDate, toDate, page, pageSize);
    }

    public async Task<ImportReceipt?> GetImportReceiptByIdAsync(int importReceiptId)
    {
        return await _receiptRepository.GetByIdWithDetailsAsync(importReceiptId);
    }

    public async Task<ImportReceipt> CreateImportReceiptAsync(int supplierId, int branchId, string? note, int userId)
    {
        int todayCount = await _receiptRepository.CountTodayAsync();
        string receiptCode = $"IMP-{DateTime.UtcNow:yyyyMMdd}-{(todayCount + 1):D4}";

        var receipt = new ImportReceipt
        {
            ReceiptCode = receiptCode,
            SupplierId = supplierId,
            BranchId = branchId,
            ImportedByUserId = userId,
            ImportDate = DateTime.UtcNow,
            Status = ImportStatus.Draft,
            Note = note,
            TotalAmount = 0m
        };

        await _receiptRepository.CreateAsync(receipt);
        await _receiptRepository.SaveChangesAsync();

        return receipt;
    }

    public async Task<ImportDetail> AddImportDetailAsync(int importReceiptId, int productId, int quantity, decimal costPrice, DateOnly? expiryDate, int userId)
    {
        var receipt = await _receiptRepository.GetByIdWithDetailsAsync(importReceiptId) 
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");

        if (receipt.Status != ImportStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể thêm chi tiết cho phiếu đang ở trạng thái Nháp (Draft).");

        if (expiryDate.HasValue && expiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            throw new InvalidOperationException("Hạn sử dụng phải lớn hơn hiện tại ít nhất 7 ngày.");

        decimal subTotal = quantity * costPrice;

        var detail = new ImportDetail
        {
            ImportReceiptId = importReceiptId,
            ProductId = productId,
            Quantity = quantity,
            CostPrice = costPrice,
            ExpiryDate = expiryDate,
            SubTotal = subTotal
        };

        await _detailRepository.AddAsync(detail);
        
        // Update total
        await _receiptRepository.UpdateTotalAmountAsync(importReceiptId, receipt.TotalAmount + subTotal);
        await _detailRepository.SaveChangesAsync();

        return detail;
    }

    public async Task ConfirmImportReceiptAsync(int importReceiptId, int userId)
    {
        var receipt = await _receiptRepository.GetByIdWithDetailsAsync(importReceiptId)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");

        if (receipt.Status != ImportStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể xác nhận phiếu đang ở trạng thái Nháp (Draft).");

        if (!receipt.ImportDetails.Any())
            throw new InvalidOperationException("Không thể xác nhận phiếu nhập chưa có sản phẩm nào.");

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Cập nhật trạng thái phiếu
            await _receiptRepository.UpdateStatusAsync(importReceiptId, ImportStatus.Confirmed, DateTime.UtcNow, userId);

            // Cập nhật tồn kho qua InventoryService
            foreach (var detail in receipt.ImportDetails)
            {
                await _inventoryService.AddStockAsync(
                    detail.ProductId,
                    receipt.BranchId,
                    detail.Quantity,
                    detail.ExpiryDate,
                    receipt.ImportReceiptId,
                    userId
                );
            }

            await _receiptRepository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task CancelImportReceiptAsync(int importReceiptId, int userId)
    {
        var receipt = await _receiptRepository.GetByIdWithDetailsAsync(importReceiptId)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");

        if (receipt.Status != ImportStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể hủy phiếu đang ở trạng thái Nháp (Draft).");

        await _receiptRepository.UpdateStatusAsync(importReceiptId, ImportStatus.Cancelled, null, null);
        await _receiptRepository.SaveChangesAsync();
    }
}
