using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Requests;

public class AiChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public int? NumberOfPeople { get; set; }
    public decimal? TargetBudget { get; set; }
    public bool PrioritizePromotions { get; set; } = true;
    public List<ChatMessageDto>? ConversationHistory { get; set; }
}
