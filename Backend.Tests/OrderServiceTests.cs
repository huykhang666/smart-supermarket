using Moq;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Orders.DTOs;
using SmartSupermarket.Backend.Features.Orders.Repositories;
using SmartSupermarket.Backend.Features.Orders.Services;
using SmartSupermarket.Backend.Features.Inventory.Repositories;
using SmartSupermarket.Backend.Features.Promotions.Repositories;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly Mock<IInventoryRepository> _mockInventoryRepo;
    private readonly Mock<IPromotionRepository> _mockPromotionRepo;
    private readonly AppDbContext _dbContext;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockInventoryRepo = new Mock<IInventoryRepository>();
        _mockPromotionRepo = new Mock<IPromotionRepository>();

        // In-memory DbContext for Products/Users/Customers lookup
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"OrderTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        // Seed a product for tests
        _dbContext.Products.Add(new Product
        {
            ProductId = 1,
            ProductName = "Coca-Cola 330ml",
            Barcode = "8934588012345",
            Price = 15000m,
            Unit = "lon",
            CategoryId = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        _orderService = new OrderService(
            _mockOrderRepo.Object,
            _mockInventoryRepo.Object,
            _mockPromotionRepo.Object,
            _dbContext
        );
    }

    // =========================================================
    // CreateOrderAsync — Happy Path
    // =========================================================

    [Fact]
    public async Task CreateOrderAsync_WithValidItems_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var inventory = new Domain.Entities.Inventory { InventoryId = 1, ProductId = 1, BranchId = 1, QuantityOnHand = 50 };
        _mockInventoryRepo.Setup(r => r.GetByProductAndBranchAsync(1, 1)).ReturnsAsync(inventory);
        _mockInventoryRepo.Setup(r => r.UpsertAsync(It.IsAny<Domain.Entities.Inventory>())).Returns(Task.CompletedTask);
        _mockInventoryRepo.Setup(r => r.AddStockHistoryAsync(It.IsAny<StockHistory>())).Returns(Task.CompletedTask);
        _mockInventoryRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), default)).Returns(Task.CompletedTask);
        _mockOrderRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _mockPromotionRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), default)).ReturnsAsync((Promotion?)null);

        var request = new CreateOrderRequest
        {
            EmployeeId = 1,
            BranchId = 1,
            Items = new List<CreateOrderDetailRequest>
            {
                new CreateOrderDetailRequest { ProductId = 1, Quantity = 2, UnitPrice = 15000m }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(30000m, result.TotalAmount);        // 2 x 15.000
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(30000m, result.FinalAmount);
        Assert.Equal(OrderStatus.Completed, result.Status);
        _mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>(), default), Times.Once);
        _mockInventoryRepo.Verify(r => r.UpsertAsync(It.IsAny<Domain.Entities.Inventory>()), Times.Once);
        _mockInventoryRepo.Verify(r => r.AddStockHistoryAsync(It.IsAny<StockHistory>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithVoucherCode_ShouldApplyDiscountCorrectly()
    {
        // Arrange
        var inventory = new Domain.Entities.Inventory { InventoryId = 1, ProductId = 1, BranchId = 1, QuantityOnHand = 50 };
        _mockInventoryRepo.Setup(r => r.GetByProductAndBranchAsync(1, 1)).ReturnsAsync(inventory);
        _mockInventoryRepo.Setup(r => r.UpsertAsync(It.IsAny<Domain.Entities.Inventory>())).Returns(Task.CompletedTask);
        _mockInventoryRepo.Setup(r => r.AddStockHistoryAsync(It.IsAny<StockHistory>())).Returns(Task.CompletedTask);
        _mockInventoryRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), default)).Returns(Task.CompletedTask);
        _mockOrderRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var promotion = new Promotion
        {
            PromotionId = 1,
            PromotionCode = "KATQ10",
            DiscountType = "Percentage",
            DiscountValue = 10m,
            MinimumOrderAmount = 0m,
            MaximumDiscountAmount = 100000m,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        _mockPromotionRepo.Setup(r => r.GetByCodeAsync("KATQ10", default)).ReturnsAsync(promotion);

        var request = new CreateOrderRequest
        {
            EmployeeId = 1,
            BranchId = 1,
            PromotionCode = "KATQ10",
            Items = new List<CreateOrderDetailRequest>
            {
                new CreateOrderDetailRequest { ProductId = 1, Quantity = 20, UnitPrice = 15000m }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert: total = 300.000, discount = 10% = 30.000, final = 270.000
        Assert.Equal(300000m, result.TotalAmount);
        Assert.Equal(30000m, result.DiscountAmount);
        Assert.Equal(270000m, result.FinalAmount);
    }

    [Fact]
    public async Task CreateOrderAsync_WithPercentageVoucher_ShouldCapAtMaximumDiscount()
    {
        // Arrange: cap max giảm = 5.000 dù 10% = 30.000
        var inventory = new Domain.Entities.Inventory { InventoryId = 1, ProductId = 1, BranchId = 1, QuantityOnHand = 50 };
        _mockInventoryRepo.Setup(r => r.GetByProductAndBranchAsync(1, 1)).ReturnsAsync(inventory);
        _mockInventoryRepo.Setup(r => r.UpsertAsync(It.IsAny<Domain.Entities.Inventory>())).Returns(Task.CompletedTask);
        _mockInventoryRepo.Setup(r => r.AddStockHistoryAsync(It.IsAny<StockHistory>())).Returns(Task.CompletedTask);
        _mockInventoryRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), default)).Returns(Task.CompletedTask);
        _mockOrderRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var promotion = new Promotion
        {
            PromotionId = 2,
            PromotionCode = "CAPTEST",
            DiscountType = "Percentage",
            DiscountValue = 10m,
            MinimumOrderAmount = 0m,
            MaximumDiscountAmount = 5000m,  // cap nhỏ
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        _mockPromotionRepo.Setup(r => r.GetByCodeAsync("CAPTEST", default)).ReturnsAsync(promotion);

        var request = new CreateOrderRequest
        {
            EmployeeId = 1, BranchId = 1, PromotionCode = "CAPTEST",
            Items = new List<CreateOrderDetailRequest>
            {
                new CreateOrderDetailRequest { ProductId = 1, Quantity = 20, UnitPrice = 15000m }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert: 10% của 300k = 30k, nhưng cap là 5k
        Assert.Equal(5000m, result.DiscountAmount);
        Assert.Equal(295000m, result.FinalAmount);
    }

    // =========================================================
    // CreateOrderAsync — Validation errors
    // =========================================================

    [Fact]
    public async Task CreateOrderAsync_WithEmptyItems_ShouldThrowArgumentException()
    {
        var request = new CreateOrderRequest { EmployeeId = 1, BranchId = 1, Items = new List<CreateOrderDetailRequest>() };
        await Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_WithNullItems_ShouldThrowArgumentException()
    {
        var request = new CreateOrderRequest { EmployeeId = 1, BranchId = 1, Items = null! };
        await Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentProduct_ShouldThrowKeyNotFoundException()
    {
        var request = new CreateOrderRequest
        {
            EmployeeId = 1, BranchId = 1,
            Items = new List<CreateOrderDetailRequest>
            {
                new CreateOrderDetailRequest { ProductId = 999, Quantity = 1, UnitPrice = 10000m }
            }
        };
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _orderService.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_WithInsufficientStock_ShouldThrowInvalidOperationException()
    {
        // Arrange: tồn kho chỉ còn 1, đặt 5
        var inventory = new Domain.Entities.Inventory { InventoryId = 1, ProductId = 1, BranchId = 1, QuantityOnHand = 1 };
        _mockInventoryRepo.Setup(r => r.GetByProductAndBranchAsync(1, 1)).ReturnsAsync(inventory);
        _mockPromotionRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), default)).ReturnsAsync((Promotion?)null);

        var request = new CreateOrderRequest
        {
            EmployeeId = 1, BranchId = 1,
            Items = new List<CreateOrderDetailRequest>
            {
                new CreateOrderDetailRequest { ProductId = 1, Quantity = 5, UnitPrice = 15000m }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _orderService.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldDeductInventoryOnSuccess()
    {
        // Arrange
        var inventory = new Domain.Entities.Inventory { InventoryId = 1, ProductId = 1, BranchId = 1, QuantityOnHand = 30 };
        _mockInventoryRepo.Setup(r => r.GetByProductAndBranchAsync(1, 1)).ReturnsAsync(inventory);
        _mockInventoryRepo.Setup(r => r.UpsertAsync(It.IsAny<Domain.Entities.Inventory>())).Returns(Task.CompletedTask);
        _mockInventoryRepo.Setup(r => r.AddStockHistoryAsync(It.IsAny<StockHistory>())).Returns(Task.CompletedTask);
        _mockInventoryRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), default)).Returns(Task.CompletedTask);
        _mockOrderRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _mockPromotionRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), default)).ReturnsAsync((Promotion?)null);

        var request = new CreateOrderRequest
        {
            EmployeeId = 1, BranchId = 1,
            Items = new List<CreateOrderDetailRequest>
            {
                new CreateOrderDetailRequest { ProductId = 1, Quantity = 5, UnitPrice = 15000m }
            }
        };

        // Act
        await _orderService.CreateOrderAsync(request);

        // Assert: tồn kho đã bị trừ 5
        Assert.Equal(25, inventory.QuantityOnHand);
        _mockInventoryRepo.Verify(r => r.AddStockHistoryAsync(
            It.Is<StockHistory>(sh => sh.ChangeType == StockChangeType.Sale && sh.QuantityChange == -5)), Times.Once);
    }

    // =========================================================
    // CancelOrderAsync
    // =========================================================

    [Fact]
    public async Task CancelOrderAsync_WithValidOrder_ShouldReturnTrue()
    {
        var order = new Order { OrderId = 1, Status = OrderStatus.Completed };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(order);
        _mockOrderRepo.Setup(r => r.Update(It.IsAny<Order>()));
        _mockOrderRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _orderService.CancelOrderAsync(1);

        Assert.True(result);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task CancelOrderAsync_WithNonExistentOrder_ShouldThrowKeyNotFoundException()
    {
        _mockOrderRepo.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Order?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _orderService.CancelOrderAsync(999));
    }

    [Fact]
    public async Task CancelOrderAsync_WithAlreadyCancelledOrder_ShouldThrowInvalidOperationException()
    {
        var order = new Order { OrderId = 1, Status = OrderStatus.Cancelled };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _orderService.CancelOrderAsync(1));
    }
}
