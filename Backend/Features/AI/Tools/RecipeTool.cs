using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public class RecipeTool : IRecipeTool
{
    private readonly IAiRecipeService _recipeService;

    public RecipeTool(IAiRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    public Task<List<MealIngredientItemDto>> GetRecipeIngredientsAsync(string dishName, int servings = 2, CancellationToken cancellationToken = default)
    {
        return _recipeService.GetRecipeIngredientsAsync(dishName, servings, cancellationToken);
    }

    public Task<string> GetCookingTipsAsync(string dishName, CancellationToken cancellationToken = default)
    {
        return _recipeService.GetCookingTipsAsync(dishName, cancellationToken);
    }
}
