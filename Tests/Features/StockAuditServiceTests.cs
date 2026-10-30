using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Inventory.DTOs;
using SmartSupermarket.Backend.Features.Inventory.Services;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class StockAuditServiceTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"StockAuditTestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAuditAsync_ValidRequest_CreatesAuditWithDraftStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);
        var request = new CreateStockAuditRequest
        {
            CreatedBy = "NV-Kho-01",
            Zone = "Kệ A1",
            Notes = "Kiểm kê định kỳ tháng 10"
        };

        // Act
        var result = await service.CreateAuditAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Draft", result.Status);
        Assert.Equal("NV-Kho-01", result.CreatedBy);
        Assert.Equal("Kệ A1", result.Zone);
        Assert.Equal(0, result.TotalItems);
    }

    [Fact]
    public async Task SaveAuditDetailAsync_NewItem_CalculatesVarianceAndAmountCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        var product = new Product
        {
            ProductId = 101,
            Barcode = "8934588012345",
            ProductName = "Bánh Chocopie Hộp 12 Cái",
            Price = 60000m,
            CostPrice = 48000m,
            Unit = "hộp",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);

        // Add 2 batches totaling 20 units
        context.InventoryBatches.AddRange(
            new InventoryBatch
            {
                BatchId = 1,
                ProductId = 101,
                BatchCode = "LOT-01",
                Quantity = 8,
                ManufacturingDate = DateTime.UtcNow.AddMonths(-2),
                ExpiryDate = DateTime.UtcNow.AddMonths(10),
                StorageLocation = "Kệ A1",
                Status = "Good"
            },
            new InventoryBatch
            {
                BatchId = 2,
                ProductId = 101,
                BatchCode = "LOT-02",
                Quantity = 12,
                ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
                ExpiryDate = DateTime.UtcNow.AddMonths(12),
                StorageLocation = "Kệ A1",
                Status = "Good"
            }
        );
        await context.SaveChangesAsync();

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ A1" });

        // Act: Đếm thực tế 15 hộp (lệch -5 hộp)
        var detailRequest = new SaveAuditDetailRequest
        {
            Barcode = "8934588012345",
            ActualQty = 15,
            LossReason = "Hàng dập nát",
            Notes = "5 hộp bị rách vỏ ngoài do vận chuyển"
        };
        var result = await service.SaveAuditDetailAsync(audit.Id, detailRequest);

        // Assert
        Assert.Equal(1, result.TotalItems);
        var detail = result.Details.First();
        Assert.Equal("8934588012345", detail.Barcode);
        Assert.Equal("Bánh Chocopie Hộp 12 Cái", detail.ProductName);
        Assert.Equal(20, detail.SystemQty);
        Assert.Equal(15, detail.ActualQty);
        Assert.Equal(-5, detail.VarianceQty);
        Assert.Equal(48000m, detail.UnitPrice);
        Assert.Equal(-240000m, detail.VarianceAmount); // -5 * 48000
        Assert.Equal("Hàng dập nát", detail.LossReason);
        Assert.Equal(240000m, result.TotalLossAmount);
        Assert.Equal(5, result.TotalDiscrepancyQty);
    }

    [Fact]
    public async Task CompleteAuditAsync_ValidAudit_SetsStatusToCompletedAndCreatesAuditLog()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);
        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { CreatedBy = "NV-Test" });

        // Act
        var result = await service.CompleteAuditAsync(audit.Id);

        // Assert
        Assert.Equal("Completed", result.Status);
        var log = await context.AuditLogs.FirstOrDefaultAsync(l => l.Action == "COMPLETE_STOCK_AUDIT");
        Assert.NotNull(log);
        Assert.Contains(audit.Id.ToString(), log.Details);
    }

    [Fact]
    public async Task ReconcileAuditAsync_UnclassifiedNegativeVariance_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        context.Products.Add(new Product
        {
            ProductId = 102,
            Barcode = "8934588055555",
            ProductName = "Nước ngọt Sting Dâu",
            Price = 12000m,
            CostPrice = 9000m,
            Unit = "chai",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        context.InventoryBatches.Add(new InventoryBatch
        {
            BatchId = 3,
            ProductId = 102,
            BatchCode = "LOT-03",
            Quantity = 10,
            ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            StorageLocation = "Kệ B1",
            Status = "Good"
        });
        await context.SaveChangesAsync();

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ B1" });
        // Actual 8 (Variance -2), but no loss reason provided
        await service.SaveAuditDetailAsync(audit.Id, new SaveAuditDetailRequest
        {
            Barcode = "8934588055555",
            ActualQty = 8,
            LossReason = "" // Chưa phân loại
        });

        // Act & Assert: Phải bị chặn và ném lỗi yêu cầu phân loại nguyên nhân hao hụt
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReconcileAuditAsync(audit.Id, new ReconcileStockAuditRequest { ReconciledBy = "Quan-Manager" }));

        Assert.Contains("nguyên nhân hao hụt", ex.Message);
    }

    [Fact]
    public async Task ReconcileAuditAsync_NegativeVarianceWithLossReason_DeductsFefoAndRecordsStockHistory()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        context.Products.Add(new Product
        {
            ProductId = 201,
            Barcode = "8935001700010",
            ProductName = "Sữa Tươi Tiệt Trùng 1L",
            Price = 38000m,
            CostPrice = 32000m,
            Unit = "hộp",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        // 2 Lô: Lô 1 cận date hơn (hết hạn sau 5 ngày, tồn 4), Lô 2 xa hơn (hết hạn sau 60 ngày, tồn 10)
        var batchNear = new InventoryBatch
        {
            BatchId = 10,
            ProductId = 201,
            BatchCode = "LOT-NEAR",
            Quantity = 4,
            ManufacturingDate = DateTime.UtcNow.AddMonths(-5),
            ExpiryDate = DateTime.UtcNow.AddDays(5),
            StorageLocation = "Kệ C1",
            Status = "Good"
        };
        var batchFar = new InventoryBatch
        {
            BatchId = 11,
            ProductId = 201,
            BatchCode = "LOT-FAR",
            Quantity = 10,
            ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = DateTime.UtcNow.AddDays(60),
            StorageLocation = "Kệ C1",
            Status = "Good"
        };
        context.InventoryBatches.AddRange(batchNear, batchFar);

        context.Inventories.Add(new SmartSupermarket.Backend.Domain.Entities.Inventory
        {
            ProductId = 201,
            BranchId = 1,
            QuantityOnHand = 14,
            MinStockLevel = 5,
            LastUpdated = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ C1" });
        // Thực tế còn 9 hộp (thiếu 5 hộp: 4 hộp lô near + 1 hộp lô far)
        await service.SaveAuditDetailAsync(audit.Id, new SaveAuditDetailRequest
        {
            Barcode = "8935001700010",
            ActualQty = 9,
            LossReason = "Hết date tiêu hủy",
            Notes = "Tiêu hủy theo biên bản số 08"
        });

        // Act: Cân bằng kho
        var reconciledAudit = await service.ReconcileAuditAsync(audit.Id, new ReconcileStockAuditRequest
        {
            ReconciledBy = "Quan-Manager",
            Notes = "Đã duyệt và tiêu hủy theo quy định"
        });

        // Assert 1: Trạng thái biên bản chuyển sang Reconciled
        Assert.Equal("Reconciled", reconciledAudit.Status);
        Assert.Equal("Quan-Manager", reconciledAudit.ReconciledBy);
        Assert.NotNull(reconciledAudit.ReconciledAt);

        // Assert 2: Tồn kho tổng hợp cập nhật về đúng 9
        var inventory = await context.Inventories.FirstAsync(i => i.ProductId == 201 && i.BranchId == 1);
        Assert.Equal(9, inventory.QuantityOnHand);

        // Assert 3: Thuật toán FEFO trừ đúng 4 từ Lô cận date và 1 từ Lô xa
        var updatedBatchNear = await context.InventoryBatches.FirstAsync(b => b.BatchId == 10);
        var updatedBatchFar = await context.InventoryBatches.FirstAsync(b => b.BatchId == 11);
        Assert.Equal(0, updatedBatchNear.Quantity); // 4 - 4 = 0
        Assert.Equal(9, updatedBatchFar.Quantity);  // 10 - 1 = 9

        // Assert 4: Ghi vết StockHistory với ChangeType = Adjustment
        var history = await context.StockHistories.FirstOrDefaultAsync(h => h.ProductId == 201);
        Assert.NotNull(history);
        Assert.Equal(StockChangeType.Adjustment, history.ChangeType);
        Assert.Equal(14, history.QuantityBefore);
        Assert.Equal(-5, history.QuantityChange);
        Assert.Equal(9, history.QuantityAfter);
        Assert.Contains("Hết date tiêu hủy", history.Note);

        // Assert 5: Ghi AuditLog
        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(l => l.Action == "RECONCILE_STOCK_AUDIT");
        Assert.NotNull(auditLog);
        Assert.Equal("Quan-Manager", auditLog.Username);
    }

    [Fact]
    public async Task ReconcileAuditAsync_PositiveVariance_AddsBatchesAndRecordsHistory()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        context.Products.Add(new Product
        {
            ProductId = 202,
            Barcode = "8935001700020",
            ProductName = "Mì Hảo Hảo Tôm Chua Cay",
            Price = 4500m,
            CostPrice = 3500m,
            Unit = "gói",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        context.InventoryBatches.Add(new InventoryBatch
        {
            BatchId = 20,
            ProductId = 202,
            BatchCode = "LOT-MII",
            Quantity = 20,
            ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = DateTime.UtcNow.AddMonths(5),
            StorageLocation = "Kệ D1",
            Status = "Good"
        });

        context.Inventories.Add(new SmartSupermarket.Backend.Domain.Entities.Inventory
        {
            ProductId = 202,
            BranchId = 1,
            QuantityOnHand = 20,
            MinStockLevel = 10,
            LastUpdated = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ D1" });
        // Thực tế kiểm đếm 25 gói (thừa +5 gói do xếp nhầm từ kệ khác)
        await service.SaveAuditDetailAsync(audit.Id, new SaveAuditDetailRequest
        {
            Barcode = "8935001700020",
            ActualQty = 25,
            LossReason = "Thừa hàng",
            Notes = "Xếp nhầm từ kệ kho sang quầy"
        });

        // Act
        var result = await service.ReconcileAuditAsync(audit.Id, new ReconcileStockAuditRequest { ReconciledBy = "NV-Kho-01" });

        // Assert
        Assert.Equal("Reconciled", result.Status);
        var inventory = await context.Inventories.FirstAsync(i => i.ProductId == 202);
        Assert.Equal(25, inventory.QuantityOnHand);

        var batch = await context.InventoryBatches.FirstAsync(b => b.BatchId == 20);
        Assert.Equal(25, batch.Quantity); // 20 + 5 = 25

        var history = await context.StockHistories.FirstOrDefaultAsync(h => h.ProductId == 202);
        Assert.NotNull(history);
        Assert.Equal(5, history.QuantityChange);
        Assert.Equal(25, history.QuantityAfter);
        Assert.Contains("+5", history.Note);
    }

    [Fact]
    public async Task ReconcileAuditAsync_AlreadyReconciled_ThrowsException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        context.Products.Add(new Product
        {
            ProductId = 203,
            Barcode = "8935001700030",
            ProductName = "Sữa đặc Ông Thọ",
            Price = 25000m,
            Unit = "lon",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ E1" });
        await service.SaveAuditDetailAsync(audit.Id, new SaveAuditDetailRequest
        {
            Barcode = "8935001700030",
            ActualQty = 10,
            LossReason = "Hàng dập nát"
        });

        await service.ReconcileAuditAsync(audit.Id, new ReconcileStockAuditRequest { ReconciledBy = "Manager" });

        // Act & Assert: Cân bằng lần 2 phải quăng ngoại lệ
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReconcileAuditAsync(audit.Id, new ReconcileStockAuditRequest { ReconciledBy = "Manager" }));
    }

    [Fact]
    public async Task GetAuditListAsync_ReturnsAllAudits()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ A" });
        await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ B" });

        // Act
        var list = await service.GetAuditListAsync();

        // Assert
        Assert.NotNull(list);
        Assert.True(list.Count >= 2);
    }

    [Fact]
    public async Task SaveAuditDetailAsync_ProductWithSales_UsesQuantityOnHandInsteadOfBatchQuantity()
    {
        // Arrange: Coca có 240 lon trong lô nhưng đã bán 4 lon (QuantityOnHand = 236)
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        context.Products.Add(new Product
        {
            ProductId = 1,
            Barcode = "8934588012015",
            ProductName = "Nước ngọt Coca-Cola 320ml",
            Price = 11000m,
            Unit = "lon",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        context.InventoryBatches.Add(new InventoryBatch
        {
            BatchId = 1,
            ProductId = 1,
            BatchCode = "LOT-COCA-01",
            Quantity = 240, // Số nhập ban đầu
            ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = DateTime.UtcNow.AddMonths(11),
            StorageLocation = "Kệ A1",
            Status = "Good"
        });

        context.Inventories.Add(new Domain.Entities.Inventory
        {
            ProductId = 1,
            BranchId = 1,
            QuantityOnHand = 236, // Đã bán 4 lon, còn 236
            MinStockLevel = 10,
            LastUpdated = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ A1" });

        // Act: Đếm đúng 236 lon thực tế
        var result = await service.SaveAuditDetailAsync(audit.Id, new SaveAuditDetailRequest
        {
            Barcode = "8934588012015",
            ActualQty = 236
        });

        // Assert: SystemQty phải là 236 (từ QuantityOnHand) chứ KHÔNG phải 240 (từ Batch), chênh lệch phải là 0
        var detail = result.Details.First();
        Assert.Equal(236, detail.SystemQty);
        Assert.Equal(236, detail.ActualQty);
        Assert.Equal(0, detail.VarianceQty);
    }

    [Fact]
    public async Task CompleteAuditAsync_BalancesStock_AndUpdatesInventoryOnHand()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        context.Products.Add(new Product
        {
            ProductId = 1,
            Barcode = "8934588012015",
            ProductName = "Nước ngọt Coca-Cola 320ml",
            Price = 11000m,
            Unit = "lon",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        context.Inventories.Add(new Domain.Entities.Inventory
        {
            ProductId = 1,
            BranchId = 1,
            QuantityOnHand = 236,
            MinStockLevel = 10,
            LastUpdated = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ A1" });
        await service.SaveAuditDetailAsync(audit.Id, new SaveAuditDetailRequest
        {
            Barcode = "8934588012015",
            ActualQty = 230 // Kiểm thực tế thiếu 6 lon
        });

        // Act: CompleteAuditAsync
        var completedAudit = await service.CompleteAuditAsync(audit.Id);

        // Assert
        Assert.Equal("Completed", completedAudit.Status);

        var inventory = await context.Inventories.FirstAsync(i => i.ProductId == 1 && i.BranchId == 1);
        Assert.Equal(230, inventory.QuantityOnHand); // Tồn kho được đồng bộ chính xác về 230

        var history = await context.StockHistories.FirstOrDefaultAsync(h => h.ProductId == 1);
        Assert.NotNull(history);
        Assert.Equal(236, history.QuantityBefore);
        Assert.Equal(-6, history.QuantityChange);
        Assert.Equal(230, history.QuantityAfter);
    }

    [Fact]
    public async Task CompleteAuditAsync_SecondCall_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        context.Products.Add(new Product
        {
            ProductId = 1,
            Barcode = "8934588012015",
            ProductName = "Nước ngọt Coca-Cola 320ml",
            Price = 11000m,
            Unit = "lon",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ A1" });
        await service.SaveAuditDetailAsync(audit.Id, new SaveAuditDetailRequest
        {
            Barcode = "8934588012015",
            ActualQty = 230
        });

        await service.CompleteAuditAsync(audit.Id);

        // Act & Assert: Chốt lần 2 phải quăng InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAuditAsync(audit.Id));
    }

    [Fact]
    public async Task SaveAuditDetailAsync_NotFoundBarcode_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new StockAuditService(context);

        var audit = await service.CreateAuditAsync(new CreateStockAuditRequest { Zone = "Kệ A1" });

        // Act & Assert: Quét barcode "000" không có trong hệ thống phải ném KeyNotFoundException (trả HTTP 404)
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SaveAuditDetailAsync(audit.Id, new SaveAuditDetailRequest
        {
            Barcode = "000",
            ActualQty = 10
        }));
    }
}

