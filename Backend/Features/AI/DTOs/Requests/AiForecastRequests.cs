using System;

namespace SmartSupermarket.Backend.Features.AI.DTOs.Requests;

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
