using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Requests;

public class ChatMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class MealPlanChatRequest
{
    public string UserPrompt { get; set; } = string.Empty;
    public decimal? TargetBudget { get; set; }
    public int? NumberOfPeople { get; set; }
    public bool PrioritizePromotions { get; set; } = true;
    public List<ChatMessageDto>? ConversationHistory { get; set; }
}
