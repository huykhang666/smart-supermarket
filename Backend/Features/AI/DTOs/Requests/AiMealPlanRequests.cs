namespace SmartSupermarket.Backend.Features.AI.DTOs.Requests;

public class MealPlanChatRequest
{
    public string UserPrompt { get; set; } = string.Empty;
    public decimal? TargetBudget { get; set; }
    public int? NumberOfPeople { get; set; }
    public bool PrioritizePromotions { get; set; } = true;
}
