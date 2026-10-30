using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.PromptVersioning.Models;

public enum PromptType
{
    SystemIdentity = 1,
    ShoppingAssistant = 2,
    RetailConcierge = 3,
    KnowledgeGrounding = 4,
    Explanation = 5,
    Fallback = 6
}

public class PromptVersionDto
{
    public string Version { get; set; } = string.Empty;
    public PromptType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int EstimatedTokens { get; set; }
}

public class PromptRollbackResultDto
{
    public bool Success { get; set; }
    public string PreviousVersion { get; set; } = string.Empty;
    public string RestoredVersion { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
