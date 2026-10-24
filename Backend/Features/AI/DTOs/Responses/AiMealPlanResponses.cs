using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

public class MealOptionDto
{
    public string OptionName { get; set; } = string.Empty;
    public string DishName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalSavedAmount { get; set; }
    public List<MealIngredientItemDto> Ingredients { get; set; } = new();
    public string CookingTips { get; set; } = string.Empty;
}

public class MealPlanChatResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string DishName { get; set; } = string.Empty;
    public int Servings { get; set; } = 4;
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalSavedAmount { get; set; }
    public bool IsWithinBudget { get; set; } = true;
    public bool HasMealSuggestion { get; set; } = false;
    public List<MealIngredientItemDto> Ingredients { get; set; } = new();
    public List<MealOptionDto> MealOptions { get; set; } = new();
    public string CookingTips { get; set; } = string.Empty;
}

public class MealIngredientItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int QuantityToBuy { get; set; } = 1;
    public decimal RegularPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsPromotionOrNearExpiry { get; set; }
    public string PromotionNote { get; set; } = string.Empty;
    public int InStockQuantity { get; set; }
}
