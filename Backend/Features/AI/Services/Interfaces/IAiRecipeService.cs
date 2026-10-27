using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Services.Interfaces;

public interface IAiRecipeService
{
    Task<List<MealIngredientItemDto>> GetRecipeIngredientsAsync(string dishName, int servings = 2, CancellationToken cancellationToken = default);
    Task<string> GetCookingTipsAsync(string dishName, CancellationToken cancellationToken = default);
}
