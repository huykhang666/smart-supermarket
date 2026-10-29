using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class PromotionIntegrationTests : IAsyncLifetime
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
    public async Task GetActiveVouchers_ShouldRetrieveOnlyCurrentlyActivePromotions()
    {
        var vouchers = await _fixture.PromotionTool.GetActiveVouchersAsync();

        Assert.NotNull(vouchers);
        Assert.Equal(3, vouchers.Count);
        Assert.Contains(vouchers, v => v.PromotionCode == "GIAM10K");
        Assert.Contains(vouchers, v => v.PromotionCode == "SALE20");
        Assert.Contains(vouchers, v => v.PromotionCode == "FRESH20");
    }

    [Fact]
    public async Task CalculateDiscount_FixedAmountVoucher_ShouldDeductWhenThresholdMet()
    {
        var items = new List<MealIngredientItemDto>
        {
            new() { ProductId = 3, ProductName = "Thịt heo xay", RegularPrice = 45000m, FinalPrice = 45000m, QuantityToBuy = 1 },
            new() { ProductId = 2, ProductName = "Rau muống", RegularPrice = 14000m, FinalPrice = 14000m, QuantityToBuy = 1 }
        }; // Total = 59,000đ >= 50,000đ min order for GIAM10K

        var discount = await _fixture.PromotionTool.CalculateDiscountAsync(items, "GIAM10K");

        Assert.Equal(10000m, discount);
    }

    [Fact]
    public async Task CalculateDiscount_PercentageVoucher_ShouldApplyCorrectPercentUpToCap()
    {
        var items = new List<MealIngredientItemDto>
        {
            new() { ProductId = 4, ProductName = "Thịt bò nạc dăm", RegularPrice = 85000m, FinalPrice = 85000m, QuantityToBuy = 1 }
        }; // 85,000 * 20% = 17,000đ (<= max cap 20,000đ)

        var discount = await _fixture.PromotionTool.CalculateDiscountAsync(items, "SALE20");

        Assert.Equal(17000m, discount);
    }

    [Fact]
    public async Task PromotionAgent_ExecuteAsync_ShouldReturnActiveVouchersAndFlashSales()
    {
        var context = new AgentContextDto
        {
            SessionId = "promo-agent-sess",
            UserPrompt = "Hôm nay siêu thị có khuyến mãi và voucher gì?",
            Intent = "PromotionQuery"
        };

        var result = await _fixture.PromotionAgent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Contains("GIAM10K", result.Output);
        Assert.Contains("FRESH20", result.Output);
    }
}
