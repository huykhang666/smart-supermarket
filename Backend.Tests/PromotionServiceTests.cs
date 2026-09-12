using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Promotions.DTOs;
using SmartSupermarket.Backend.Features.Promotions.Repositories;
using SmartSupermarket.Backend.Features.Promotions.Services;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class PromotionServiceTests
{
    private readonly Mock<IPromotionRepository> _mockRepo;
    private readonly PromotionService _service;

    public PromotionServiceTests()
    {
        _mockRepo = new Mock<IPromotionRepository>();
        _service = new PromotionService(_mockRepo.Object);
    }

    // =========================================================
    // ApplyPromotionAsync — Happy Paths
    // =========================================================

    [Fact]
    public async Task ApplyPromotion_WithValidPercentageCode_ShouldApplyDiscount()
    {
        // Arrange: 10% giảm, tổng đơn 500.000, không có trần
        var promotion = MakePromotion("KATQ10", "Percentage", 10m, 0m, null);
        _mockRepo.Setup(r => r.GetByCodeAsync("KATQ10", default)).ReturnsAsync(promotion);

        var request = new ApplyPromotionRequest { PromotionCode = "KATQ10", OrderTotalAmount = 500000m };

        // Act
        var result = await _service.ApplyPromotionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50000m, result.DiscountAmount);     // 10% x 500k
        Assert.Equal(450000m, result.FinalAmount);
    }

    [Fact]
    public async Task ApplyPromotion_WithPercentageAndMaxCap_ShouldCapDiscount()
    {
        // Arrange: 10% giảm, trần 20.000 — 10% của 500k = 50k, nhưng cap là 20k
        var promotion = MakePromotion("CAPTEST", "Percentage", 10m, 0m, 20000m);
        _mockRepo.Setup(r => r.GetByCodeAsync("CAPTEST", default)).ReturnsAsync(promotion);

        var request = new ApplyPromotionRequest { PromotionCode = "CAPTEST", OrderTotalAmount = 500000m };

        // Act
        var result = await _service.ApplyPromotionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20000m, result.DiscountAmount);
        Assert.Equal(480000m, result.FinalAmount);
    }

    [Fact]
    public async Task ApplyPromotion_WithFixedAmountCode_ShouldApplyFixedDiscount()
    {
        // Arrange: giảm cố định 30.000
        var promotion = MakePromotion("FREESHIP", "FixedAmount", 30000m, 0m, null);
        _mockRepo.Setup(r => r.GetByCodeAsync("FREESHIP", default)).ReturnsAsync(promotion);

        var request = new ApplyPromotionRequest { PromotionCode = "FREESHIP", OrderTotalAmount = 600000m };

        var result = await _service.ApplyPromotionAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(30000m, result.DiscountAmount);
        Assert.Equal(570000m, result.FinalAmount);
    }

    [Fact]
    public async Task ApplyPromotion_DiscountCannotExceedOrderTotal()
    {
        // Arrange: giảm cố định 200k, nhưng đơn hàng chỉ 100k → discount bị cap ở 100k
        var promotion = MakePromotion("BIGCUT", "FixedAmount", 200000m, 0m, null);
        _mockRepo.Setup(r => r.GetByCodeAsync("BIGCUT", default)).ReturnsAsync(promotion);

        var request = new ApplyPromotionRequest { PromotionCode = "BIGCUT", OrderTotalAmount = 100000m };

        var result = await _service.ApplyPromotionAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(100000m, result.DiscountAmount); // capped at order total
        Assert.Equal(0m, result.FinalAmount);
    }

    // =========================================================
    // ApplyPromotionAsync — Failure cases
    // =========================================================

    [Fact]
    public async Task ApplyPromotion_WithNonExistentCode_ShouldReturnFailure()
    {
        _mockRepo.Setup(r => r.GetByCodeAsync("NONEXIST", default)).ReturnsAsync((Promotion?)null);

        var request = new ApplyPromotionRequest { PromotionCode = "NONEXIST", OrderTotalAmount = 500000m };
        var result = await _service.ApplyPromotionAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(500000m, result.FinalAmount);
    }

    [Fact]
    public async Task ApplyPromotion_WithInactiveCode_ShouldReturnFailure()
    {
        var promotion = MakePromotion("INACTIVE", "Percentage", 10m, 0m, null, isActive: false);
        _mockRepo.Setup(r => r.GetByCodeAsync("INACTIVE", default)).ReturnsAsync(promotion);

        var request = new ApplyPromotionRequest { PromotionCode = "INACTIVE", OrderTotalAmount = 500000m };
        var result = await _service.ApplyPromotionAsync(request);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApplyPromotion_WithExpiredCode_ShouldReturnFailure()
    {
        var promotion = MakePromotion("EXPIRED", "Percentage", 10m, 0m, null,
            startDate: DateTime.UtcNow.AddDays(-30), endDate: DateTime.UtcNow.AddDays(-1));
        _mockRepo.Setup(r => r.GetByCodeAsync("EXPIRED", default)).ReturnsAsync(promotion);

        var request = new ApplyPromotionRequest { PromotionCode = "EXPIRED", OrderTotalAmount = 500000m };
        var result = await _service.ApplyPromotionAsync(request);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApplyPromotion_WithFutureCode_ShouldReturnFailure()
    {
        var promotion = MakePromotion("FUTURE", "Percentage", 10m, 0m, null,
            startDate: DateTime.UtcNow.AddDays(5), endDate: DateTime.UtcNow.AddDays(30));
        _mockRepo.Setup(r => r.GetByCodeAsync("FUTURE", default)).ReturnsAsync(promotion);

        var request = new ApplyPromotionRequest { PromotionCode = "FUTURE", OrderTotalAmount = 500000m };
        var result = await _service.ApplyPromotionAsync(request);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApplyPromotion_WithOrderBelowMinimum_ShouldReturnFailure()
    {
        // Arrange: đơn tối thiểu 500k, nhưng chỉ đặt 200k
        var promotion = MakePromotion("MINORDER", "Percentage", 10m, minimumOrder: 500000m, maxCap: null);
        _mockRepo.Setup(r => r.GetByCodeAsync("MINORDER", default)).ReturnsAsync(promotion);

        var request = new ApplyPromotionRequest { PromotionCode = "MINORDER", OrderTotalAmount = 200000m };
        var result = await _service.ApplyPromotionAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(200000m, result.FinalAmount);
    }

    // =========================================================
    // CreateAsync / UpdateAsync / DeleteAsync
    // =========================================================

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ShouldThrowInvalidOperationException()
    {
        var existing = MakePromotion("DUPCODE", "Percentage", 10m, 0m, null);
        _mockRepo.Setup(r => r.GetByCodeAsync("DUPCODE", default)).ReturnsAsync(existing);

        var request = new CreatePromotionRequest
        {
            PromotionCode = "DUPCODE", PromotionName = "Test",
            DiscountType = "Percentage", DiscountValue = 10m,
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), IsActive = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldThrowKeyNotFoundException()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Promotion?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(999));
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ShouldThrowKeyNotFoundException()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Promotion?)null);

        var request = new UpdatePromotionRequest
        {
            PromotionName = "X", DiscountType = "Percentage", DiscountValue = 5m,
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), IsActive = true
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(999, request));
    }

    // =========================================================
    // Helper factory
    // =========================================================

    private static Promotion MakePromotion(
        string code, string discountType, decimal discountValue,
        decimal minimumOrder, decimal? maxCap,
        bool isActive = true,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        return new Promotion
        {
            PromotionId = 1,
            PromotionCode = code,
            PromotionName = $"Test Promotion {code}",
            DiscountType = discountType,
            DiscountValue = discountValue,
            MinimumOrderAmount = minimumOrder,
            MaximumDiscountAmount = maxCap,
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-1),
            EndDate = endDate ?? DateTime.UtcNow.AddDays(30),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
