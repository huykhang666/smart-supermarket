using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Reports.Controllers;
using SmartSupermarket.Backend.Features.Reports.DTOs;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class ReportControllerTests
{
    private readonly AppDbContext _dbContext;
    private readonly ReportController _controller;

    public ReportControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ReportTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _controller = new ReportController(_dbContext);
    }

    [Fact]
    public async Task GetRevenueProfit_CalculatesRevenueCostAndProfitCorrectly()
    {
        // Arrange: 1 Category, 1 Product with CostPrice = 10,000 and Price = 15,000
        var cat = new Category { CategoryId = 1, CategoryName = "Đồ uống", Slug = "do-uong" };
        var prod = new Product
        {
            ProductId = 1,
            ProductName = "Trà Oolong",
            Barcode = "8931112223334",
            CategoryId = 1,
            Price = 15000m,
            CostPrice = 10000m,
            Status = ProductStatus.Active
        };
        await _dbContext.Categories.AddAsync(cat);
        await _dbContext.Products.AddAsync(prod);

        // Order today VN
        var order = new Order
        {
            OrderId = 1,
            EmployeeId = 1,
            BranchId = 1,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 30000m,
            DiscountAmount = 5000m,
            FinalAmount = 25000m,
            Status = OrderStatus.Completed,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail
                {
                    OrderDetailId = 1,
                    ProductId = 1,
                    Quantity = 2,
                    UnitPrice = 15000m,
                    SubTotal = 30000m
                }
            },
            Payments = new List<Payment>
            {
                new Payment
                {
                    PaymentId = 1,
                    OrderId = 1,
                    PaymentMethod = PaymentMethod.Cash,
                    AmountPaid = 25000m
                }
            }
        };
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();

        // Act
        var actionResult = await _controller.GetRevenueProfit(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var apiResult = Assert.IsType<ApiResult<RevenueProfitReportDto>>(okResult.Value);
        Assert.True(apiResult.IsSuccess);
        var report = apiResult.Data;
        Assert.NotNull(report);

        Assert.Equal(1, report.TotalOrders);
        Assert.Equal(25000m, report.TotalRevenue);
        // Cost: 2 * 10,000 = 20,000
        Assert.Equal(20000m, report.TotalCost);
        // Gross Profit: 25,000 - 20,000 = 5,000
        Assert.Equal(5000m, report.GrossProfit);
        Assert.Equal(20m, report.ProfitMargin); // 5000 / 25000 * 100 = 20%
        Assert.Equal(25000m, report.PaymentBreakdown.Cash);
    }

    [Fact]
    public async Task GetRevenueProfit_WithStockLoss_DeductsLossFromGrossProfit()
    {
        // Arrange
        var cat = new Category { CategoryId = 3, CategoryName = "Thực phẩm", Slug = "thuc-pham" };
        var prod = new Product
        {
            ProductId = 20,
            ProductName = "Sữa Tươi",
            Barcode = "893999999",
            CategoryId = 3,
            Price = 20000m,
            CostPrice = 14000m,
            Status = ProductStatus.Active
        };
        await _dbContext.Categories.AddAsync(cat);
        await _dbContext.Products.AddAsync(prod);

        var order = new Order
        {
            OrderId = 201,
            EmployeeId = 1,
            BranchId = 1,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 20000m,
            FinalAmount = 20000m,
            Status = OrderStatus.Completed,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { OrderDetailId = 201, ProductId = 20, Quantity = 1, UnitPrice = 20000m, SubTotal = 20000m }
            }
        };
        await _dbContext.Orders.AddAsync(order);

        // Biên bản kiểm kê có 1 mặt hàng thất thoát/hư hỏng (VarianceQty = -1, UnitPrice = 2000 => Loss = 2000)
        var audit = new StockAudit
        {
            Id = Guid.NewGuid(),
            CreatedBy = "Admin",
            Zone = "A",
            AuditDate = DateTime.UtcNow,
            Status = "Completed",
            Details = new List<StockAuditDetail>
            {
                new StockAuditDetail
                {
                    Id = Guid.NewGuid(),
                    Barcode = "893999999",
                    SystemQty = 5,
                    ActualQty = 4, // Thiếu 1
                    UnitPrice = 2000m, // Hao hụt 2,000đ
                    LossReason = "Hàng dập nát"
                }
            }
        };
        await _dbContext.StockAudits.AddAsync(audit);
        await _dbContext.SaveChangesAsync();

        // Act
        var actionResult = await _controller.GetRevenueProfit(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var apiResult = Assert.IsType<ApiResult<RevenueProfitReportDto>>(okResult.Value);
        var report = apiResult.Data;
        Assert.NotNull(report);

        Assert.Equal(2000m, report.TotalLossAmount);
        // Doanh thu (20k) - Giá vốn (14k) - Hao hụt (2k) = 4,000đ
        Assert.Equal(4000m, report.GrossProfit);
    }

    [Fact]
    public async Task GetBestSellers_ReturnsTopProductsOrderedByQuantity()
    {
        // Arrange: 2 products
        var cat = new Category { CategoryId = 2, CategoryName = "Bánh kẹo", Slug = "banh-keo" };
        var p1 = new Product { ProductId = 10, ProductName = "Bánh Chocopie", Barcode = "8930001", Price = 50000m, CategoryId = 2 };
        var p2 = new Product { ProductId = 11, ProductName = "Kẹo Alpenliebe", Barcode = "8930002", Price = 20000m, CategoryId = 2 };
        await _dbContext.Categories.AddAsync(cat);
        await _dbContext.Products.AddRangeAsync(p1, p2);

        // Add inventory
        await _dbContext.Inventories.AddRangeAsync(
            new Inventory { ProductId = 10, BranchId = 1, QuantityOnHand = 80 },
            new Inventory { ProductId = 11, BranchId = 1, QuantityOnHand = 120 }
        );

        // Order 1: 5 p1
        // Order 2: 12 p2
        var o1 = new Order
        {
            OrderId = 101,
            BranchId = 1,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Completed,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { ProductId = 10, Quantity = 5, UnitPrice = 50000m, SubTotal = 250000m }
            }
        };
        var o2 = new Order
        {
            OrderId = 102,
            BranchId = 1,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Completed,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { ProductId = 11, Quantity = 12, UnitPrice = 20000m, SubTotal = 240000m }
            }
        };
        await _dbContext.Orders.AddRangeAsync(o1, o2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetBestSellers(top: 10, days: 7, branchId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResult = Assert.IsType<ApiResult<List<BestSellerItemDto>>>(okResult.Value);
        Assert.True(apiResult.IsSuccess);
        var list = apiResult.Data;
        Assert.NotNull(list);
        Assert.Equal(2, list.Count);

        // p2 sold 12 items -> first
        Assert.Equal(11, list[0].ProductId);
        Assert.Equal(12, list[0].QuantitySold);
        Assert.Equal(120, list[0].CurrentStock);

        // p1 sold 5 items -> second
        Assert.Equal(10, list[1].ProductId);
        Assert.Equal(5, list[1].QuantitySold);
        Assert.Equal(80, list[1].CurrentStock);
    }

    [Fact]
    public async Task GetInventoryExpiry_IdentifiesExpiredAndExpiringBatches()
    {
        // Arrange
        var cat = new Category { CategoryId = 1, CategoryName = "Sữa", Slug = "sua" };
        var prod = new Product { ProductId = 20, ProductName = "Sữa Tươi 1L", Barcode = "8939999", Price = 35000m, CategoryId = 1, Category = cat };
        await _dbContext.Categories.AddAsync(cat);
        await _dbContext.Products.AddAsync(prod);

        // Batch 1: Expired yesterday
        var b1 = new InventoryBatch
        {
            BatchId = 1,
            ProductId = 20,
            Product = prod,
            BatchCode = "LOT-01",
            Quantity = 4,
            ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = DateTime.UtcNow.AddDays(-1),
            Status = "Expired"
        };
        // Batch 2: Expiring in 3 days (within 5-day threshold)
        var b2 = new InventoryBatch
        {
            BatchId = 2,
            ProductId = 20,
            Product = prod,
            BatchCode = "LOT-02",
            Quantity = 10,
            ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = DateTime.UtcNow.AddDays(3),
            Status = "Warning"
        };
        // Batch 3: Expiring in 20 days (outside 5-day threshold)
        var b3 = new InventoryBatch
        {
            BatchId = 3,
            ProductId = 20,
            Product = prod,
            BatchCode = "LOT-03",
            Quantity = 25,
            ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = DateTime.UtcNow.AddDays(20),
            Status = "Good"
        };

        await _dbContext.InventoryBatches.AddRangeAsync(b1, b2, b3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetInventoryExpiry(daysThreshold: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResult = Assert.IsType<ApiResult<InventoryExpiryReportDto>>(okResult.Value);
        Assert.True(apiResult.IsSuccess);
        var report = apiResult.Data;
        Assert.NotNull(report);

        Assert.Equal(2, report.ExpiringBatches.Count);
        Assert.Equal(4, report.TotalExpiredItems);
        Assert.Equal(10, report.TotalExpiringItems);
    }

    [Fact]
    public async Task GetCashierShiftReport_SeparatesCashAndQrAndCalculatesTotals()
    {
        // Arrange
        var cashier = new User
        {
            UserId = 5,
            Username = "cashier01",
            FullName = "Nguyễn Văn Thu Ngân",
            Email = "cashier01@smartmarket.vn",
            PasswordHash = "hash"
        };
        await _dbContext.Users.AddAsync(cashier);

        var oCash = new Order
        {
            OrderId = 201,
            EmployeeId = 5,
            BranchId = 1,
            OrderDate = DateTime.UtcNow,
            FinalAmount = 100000m,
            Status = OrderStatus.Completed,
            Payments = new List<Payment>
            {
                new Payment { PaymentMethod = PaymentMethod.Cash, AmountPaid = 100000m }
            }
        };

        var oQr = new Order
        {
            OrderId = 202,
            EmployeeId = 5,
            BranchId = 1,
            OrderDate = DateTime.UtcNow,
            FinalAmount = 150000m,
            Status = OrderStatus.Completed,
            Payments = new List<Payment>
            {
                new Payment { PaymentMethod = PaymentMethod.QRCode, AmountPaid = 150000m }
            }
        };

        await _dbContext.Orders.AddRangeAsync(oCash, oQr);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCashierShiftReport(DateTime.UtcNow.AddHours(7).Date, 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResult = Assert.IsType<ApiResult<CashierShiftReportDto>>(okResult.Value);
        Assert.True(apiResult.IsSuccess);
        var report = apiResult.Data;
        Assert.NotNull(report);

        Assert.Equal(2, report.TotalOrders);
        Assert.Equal(250000m, report.TotalRevenue);
        Assert.Equal(100000m, report.TotalCash);
        Assert.Equal(150000m, report.TotalQr);
        Assert.Single(report.Cashiers);
        Assert.Equal("Nguyễn Văn Thu Ngân", report.Cashiers[0].FullName);
    }
}
