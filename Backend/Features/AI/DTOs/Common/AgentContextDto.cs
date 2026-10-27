using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public class AgentContextDto
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public string Intent { get; set; } = "MealPlanning";
    public int Servings { get; set; } = 2;
    public decimal TargetBudget { get; set; } = 350000m;
    public string? DietaryPreference { get; set; }
    public List<string> ExcludedIngredients { get; set; } = new();
    public List<int> CurrentCartItemProductIds { get; set; } = new();
    public Dictionary<string, object> SharedData { get; set; } = new();
}
