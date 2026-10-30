using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public enum IntentType
{
    MealPlanning,
    ProductSearch,
    PromotionInquiry,
    CartAction,
    CulinaryAdvice,
    Greeting,
    GeneralChat,
    // Phase 2: Retail Shopping Intents
    ProductRecommendation,
    CategoryComparison,
    PriceComparison,
    QualityComparison,
    BrandComparison,
    BudgetShopping,
    ValueForMoneyShopping,
    DurableProductSearch,
    PremiumProductSearch,
    CheapProductSearch,
    PromotionAwareShopping,
    InventoryAwareShopping
}

public class UserIntentDto
{
    public IntentType Type { get; set; } = IntentType.GeneralChat;
    public double Confidence { get; set; } = 1.0;
    public string RawPrompt { get; set; } = string.Empty;
    public int? Servings { get; set; }
    public decimal? TargetBudget { get; set; }
    public string? DishTheme { get; set; }
    public List<string> Keywords { get; set; } = new();
    public List<int> CategoryIds { get; set; } = new();
    public bool PrioritizePromotions { get; set; } = true;

    // Phase 2: Retail Shopping Extensions
    public string? RetailCategory { get; set; }
    public string? RetailUseCase { get; set; }
    public string? PreferredTier { get; set; }
}
