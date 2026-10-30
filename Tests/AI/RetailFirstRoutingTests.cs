using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.Services.Intent;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class RetailFirstRoutingTests
{
    private readonly IntentAnalyzer _intentAnalyzer = new();

    [Theory]
    [InlineData("có nồi cơm 500k không", IntentType.BudgetShopping, "Đồ dùng gia đình & Nhà bếp")]
    [InlineData("mua dầu gội nào rẻ mà ổn", IntentType.CheapProductSearch, "Chăm sóc cá nhân")]
    [InlineData("nước rửa chén nào tiết kiệm mà tốt", IntentType.ValueForMoneyShopping, "Hóa phẩm & Giặt xả")]
    [InlineData("giấy vệ sinh loại nào đáng tiền", IntentType.ValueForMoneyShopping, "Giấy vệ sinh & Khăn giấy")]
    [InlineData("mua chảo 300k", IntentType.BudgetShopping, "Đồ dùng gia đình & Nhà bếp")]
    public async Task RetailQuestions_ClassifiedAsRetail_NotMealPlanning(string prompt, IntentType expectedType, string expectedCategory)
    {
        var result = await _intentAnalyzer.AnalyzeIntentAsync(prompt);

        Assert.NotNull(result);
        Assert.Equal(expectedType, result.Type);
        Assert.Equal(expectedCategory, result.RetailCategory);
    }

    [Theory]
    [InlineData("Mình có 300k, nấu lẩu bò cho 4 người.")]
    [InlineData("nấu ăn cho 2 người hết bao nhiêu tiền")]
    [InlineData("hôm nay ăn món gì")]
    [InlineData("gợi ý mâm cơm gia đình")]
    public async Task CookingQuestions_ClassifiedAsMealPlanning(string prompt)
    {
        var result = await _intentAnalyzer.AnalyzeIntentAsync(prompt);

        Assert.NotNull(result);
        Assert.Equal(IntentType.MealPlanning, result.Type);
    }
}
