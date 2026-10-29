using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Repositories;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Services.Implementations;

public class AiRecipeService : IAiRecipeService
{
    private readonly IAiRecipeRepository _recipeRepository;
    private readonly IKnowledgeCache _knowledgeCache;

    public AiRecipeService(
        IAiRecipeRepository recipeRepository,
        IKnowledgeCache knowledgeCache)
    {
        _recipeRepository = recipeRepository;
        _knowledgeCache = knowledgeCache;
    }

    public async Task<List<MealIngredientItemDto>> GetRecipeIngredientsAsync(
        string dishName,
        int servings = 2,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"recipe:ingredients:{dishName.Trim().ToLowerInvariant()}:{servings}";
        return await _knowledgeCache.GetOrSetKnowledgeAsync(cacheKey, async ct =>
        {
            var products = await _recipeRepository.GetRecipeIngredientsAsync(dishName, servings, ct);
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
        }, expiration: TimeSpan.FromHours(24), cancellationToken: cancellationToken);
    }

    public async Task<string> GetCookingTipsAsync(string dishName, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"recipe:tips:{dishName.Trim().ToLowerInvariant()}";
        return await _knowledgeCache.GetOrSetKnowledgeAsync(cacheKey, async ct =>
        {
            var tips = await _recipeRepository.GetCookingTipsAsync(dishName, ct);
            return tips ?? "Nấu ăn vui vẻ, chúc bạn và gia đình có một bữa ăn ngon miệng!";
        }, expiration: TimeSpan.FromHours(24), cancellationToken: cancellationToken);
    }
}
