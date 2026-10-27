using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Repositories;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Services.Implementations;

public class AiRecipeService : IAiRecipeService
{
    private readonly IAiRecipeRepository _recipeRepository;

    public AiRecipeService(IAiRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<List<MealIngredientItemDto>> GetRecipeIngredientsAsync(
        string dishName,
        int servings = 2,
        CancellationToken cancellationToken = default)
    {
        var products = await _recipeRepository.GetRecipeIngredientsAsync(dishName, servings, cancellationToken);
        var result = new List<MealIngredientItemDto>();

        foreach (var p in products)
        {
            int qty = servings > 3 ? 2 : 1;
            result.Add(new MealIngredientItemDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Barcode = p.Barcode ?? string.Empty,
                ImageUrl = p.ImageUrl ?? string.Empty,
                Unit = p.Unit ?? "phần",
                QuantityToBuy = qty,
                RegularPrice = p.Price,
                FinalPrice = p.Price,
                DiscountPercent = 0,
                InStockQuantity = 10
            });
        }

        return result;
    }

    public async Task<string> GetCookingTipsAsync(string dishName, CancellationToken cancellationToken = default)
    {
        var tips = await _recipeRepository.GetCookingTipsAsync(dishName, cancellationToken);
        return tips ?? "Nấu ăn vui vẻ, chúc bạn và gia đình có một bữa ăn ngon miệng!";
    }
}
