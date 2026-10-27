using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

public class AiChatOptionCardDto
{
    public int OptionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal SavedAmount { get; set; }
    public int ItemCount { get; set; }
    public List<MealIngredientItemDto> Ingredients { get; set; } = new();
}

public class AiChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public bool HasMealSuggestion { get; set; }
    public decimal TotalEstimatedPrice { get; set; }
    public decimal TotalSavedAmount { get; set; }
    public int Servings { get; set; } = 2;
    public List<AiChatOptionCardDto> OptionCards { get; set; } = new();
    public string CookingTips { get; set; } = string.Empty;
    public string AgentUsed { get; set; } = "ShoppingAgent";
    public string SourceProvider { get; set; } = "Gemini-2.5-Flash";
}
