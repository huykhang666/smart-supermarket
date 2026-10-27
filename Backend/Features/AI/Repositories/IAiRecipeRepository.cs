using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.AI.Repositories;

public interface IAiRecipeRepository
{
    Task<List<Product>> GetRecipeIngredientsAsync(
        string dishName,
        int servings,
        CancellationToken cancellationToken = default);

    Task<string?> GetCookingTipsAsync(
        string dishName,
        CancellationToken cancellationToken = default);
}
