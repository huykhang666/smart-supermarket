using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Requests;

public class AiAgentExecuteRequestDto
{
    public string AgentName { get; set; } = "ShoppingAgent";
    public string Prompt { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public int? Servings { get; set; }
    public decimal? TargetBudget { get; set; }
    public string? Intent { get; set; }
    public List<int>? ProductIds { get; set; }
}
