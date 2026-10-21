using System;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Responses;

public class AiResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
