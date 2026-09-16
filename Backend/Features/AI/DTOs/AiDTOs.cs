namespace SmartSupermarket.Backend.Features.AI.DTOs;

public class AiImportForecastRequest
{
    public int DaysBack { get; set; } = 30;
    public int? BranchId { get; set; }
}

public class AiRevenueReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class AiProductAnalysisRequest
{
    public int TopN { get; set; } = 10;
    public int DaysBack { get; set; } = 30;
}

public class AiResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
