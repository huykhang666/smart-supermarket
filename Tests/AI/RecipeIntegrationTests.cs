using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using Xunit;

namespace SmartSupermarket.Backend.Tests.AI;

public class RecipeIntegrationTests : IAsyncLifetime
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
    public async Task GetRecipeIngredients_ShouldQueryMatchingProductsFromDatabase()
    {
        var ingredients = await _fixture.RecipeTool.GetRecipeIngredientsAsync("Thịt kho tàu", servings: 2);

        Assert.NotEmpty(ingredients);
        Assert.Contains(ingredients, i => i.ProductName.Contains("Thịt"));
        Assert.Contains(ingredients, i => i.ProductName.Contains("Trứng"));
    }

    [Fact]
    public async Task GetRecipeIngredients_ShouldScaleQuantityWhenServingsOverThree()
    {
        var ingredientsFor2 = await _fixture.RecipeTool.GetRecipeIngredientsAsync("Thịt kho", servings: 2);
        var ingredientsFor4 = await _fixture.RecipeTool.GetRecipeIngredientsAsync("Thịt kho", servings: 4);

        Assert.NotEmpty(ingredientsFor2);
        Assert.NotEmpty(ingredientsFor4);
        Assert.Equal(1, ingredientsFor2[0].QuantityToBuy);
        Assert.Equal(2, ingredientsFor4[0].QuantityToBuy);
    }

    [Fact]
    public async Task RecipeCookingTips_ShouldCacheInKnowledgeCache()
    {
        var dishName = "Cơm trứng chiên";

        // First call populates knowledge cache
        var tips1 = await _fixture.RecipeTool.GetCookingTipsAsync(dishName);
        Assert.False(string.IsNullOrWhiteSpace(tips1));

        // Direct check in KnowledgeCache
        var cacheKey = $"recipe:tips:{dishName.Trim().ToLowerInvariant()}";
        var cachedTips = await _fixture.KnowledgeCache.GetKnowledgeAsync<string>(cacheKey);

        Assert.Equal(tips1, cachedTips);

        // Second call retrieves from cache seamlessly
        var tips2 = await _fixture.RecipeTool.GetCookingTipsAsync(dishName);
        Assert.Equal(tips1, tips2);
    }

    [Fact]
    public async Task RecipeAgent_ExecuteAsync_ShouldReturnCookpadIngredientsAndTips()
    {
        var context = new AgentContextDto
        {
            SessionId = "recipe-sess",
            UserPrompt = "Tìm công thức món Thịt kho tàu cho 4 người",
            Intent = "RecipeSearch",
            Servings = 4
        };

        var result = await _fixture.RecipeAgent.ExecuteAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Contains("Thịt", result.Output);
        Assert.Contains("người ăn", result.Output);
    }
}
