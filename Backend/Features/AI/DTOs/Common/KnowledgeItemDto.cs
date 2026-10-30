using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Common;

public enum KnowledgeType
{
    FaqPolicy = 1,
    CategoryProfile = 2,
    RecipeGuide = 3,
    DietaryNutrition = 4,
    General = 5
}

public class KnowledgeChunkDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public KnowledgeType Type { get; set; } = KnowledgeType.General;
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public string SourceDoc { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

public class KnowledgeQueryResultDto
{
    public string Query { get; set; } = string.Empty;
    public List<KnowledgeChunkDto> Chunks { get; set; } = new();
    public bool HasRelevantKnowledge => Chunks.Count > 0;
    public string? PrimarySource => Chunks.Count > 0 ? Chunks[0].SourceDoc : null;
}
