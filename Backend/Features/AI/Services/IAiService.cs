using SmartSupermarket.Backend.Features.AI.DTOs;

namespace SmartSupermarket.Backend.Features.AI.Services;

public interface IAiService
{
    Task<AiResponse> GenerateImportForecastAsync(AiImportForecastRequest request, CancellationToken cancellationToken = default);
    Task<AiResponse> GenerateRevenueReportAsync(AiRevenueReportRequest request, CancellationToken cancellationToken = default);
    Task<AiResponse> GenerateProductAnalysisAsync(AiProductAnalysisRequest request, CancellationToken cancellationToken = default);
}
