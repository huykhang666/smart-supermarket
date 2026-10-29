using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class InventoryIntegrationTests : IAsyncLifetime
{
    private readonly AiIntegrationTestFixture _fixture = new();

    public async Task InitializeAsync()
    {
        await _fixture.SeedSampleDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task GetNearExpiryBatches_ShouldDetectNearExpiryItemsAndCalculateDiscount()
    {
        // Act: Rà soát các lô hàng cận hạn trong vòng 3 ngày
        var nearExpiryList = await _fixture.InventoryTool.GetNearExpiryBatchesAsync(daysThreshold: 3);

        // Assert: Lô Rau muống nước (Batch 1, còn 2 ngày) phải xuất hiện
        Assert.NotEmpty(nearExpiryList);
        var rauBatch = nearExpiryList.Find(b => b.ProductId == 2);
        Assert.NotNull(rauBatch);
        Assert.Equal("Rau muống nước VietGAP", rauBatch.ProductName);
        Assert.Equal(15, rauBatch.Quantity);
        Assert.True(rauBatch.DaysRemaining <= 3);
        Assert.Equal(30m, rauBatch.DiscountPercent); // Áp dụng DiscountRule cận 3 ngày giảm 30%
        Assert.Equal(9800m, rauBatch.FlashSalePrice); // 14,000 * 0.7 = 9,800
    }

    [Fact]
    public async Task GetStock_ShouldAggregateTotalQuantityAcrossBatches()
    {
        // Act: Lấy tổng tồn kho Trứng gà (Batch 2 có 50 quả)
        var stock = await _fixture.InventoryTool.GetStockAsync(productId: 1);

        // Assert
        Assert.Equal(50, stock);
    }

    [Fact]
    public async Task InventoryAgent_NearExpiryAudit_ShouldExecuteEndToEndThroughToolDispatcher()
    {
        var context = new AgentContextDto
        {
            SessionId = "inv-audit-sess",
            UserPrompt = "Kiểm tra hàng cận date và hạn sử dụng",
            Intent = "NearExpiryAudit"
        };

        var result = await _fixture.InventoryAgent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Contains("Rau muống nước VietGAP", result.Output);
        Assert.True(result.ExecutionTimeMs >= 0);
    }
}
