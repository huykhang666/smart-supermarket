using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Tools;

public interface IRecipeTool
{
    Task<List<MealIngredientItemDto>> GetRecipeIngredientsAsync(string dishName, int servings = 2, CancellationToken cancellationToken = default);
    Task<string> GetCookingTipsAsync(string dishName, CancellationToken cancellationToken = default);
}
