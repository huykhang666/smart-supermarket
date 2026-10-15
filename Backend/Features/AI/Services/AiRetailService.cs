using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Services;

/// <summary>
/// Khung cấu trúc triển khai nghiệp vụ phân tích AI bán lẻ (AI Retail Intelligence).
/// Kết nối hạ tầng ILlmClient (Groq/Gemini) với cơ sở dữ liệu AppDbContext.
/// </summary>
public class AiRetailService : IAiRetailService
{
    private readonly AppDbContext _dbContext;
    private readonly ILlmClient _llmClient;

    public AiRetailService(AppDbContext dbContext, ILlmClient llmClient)
    {
        _dbContext = dbContext;
        _llmClient = llmClient;
    }

    public async Task<AiProductRiskReport> AnalyzeProductRisksAsync(CancellationToken cancellationToken = default)
    {
        // Khung cấu trúc xử lý:
        // 1. Lọc sản phẩm có ExpiryDate sắp tới hoặc tồn kho thấp từ _dbContext
        // 2. Gom dữ liệu gửi sang _llmClient yêu cầu xuất JSON
        // 3. Deserialize JSON thành AiProductRiskReport
        await Task.CompletedTask;
        return new AiProductRiskReport
        {
            SummaryStrategy = "Khung cấu trúc sẵn sàng kết nối dữ liệu thật."
        };
    }

    public async Task<AiMarketBasketReport> AnalyzeMarketBasketAsync(int daysBack = 30, CancellationToken cancellationToken = default)
    {
        // Khung cấu trúc xử lý:
        // 1. Quét bảng OrderDetails trong 'daysBack' ngày
        // 2. Phân tích các cặp sản phẩm thường mua cùng
        // 3. AI sinh gói combo và tỷ lệ giảm giá phù hợp
        await Task.CompletedTask;
        return new AiMarketBasketReport
        {
            GeneralRecommendations = "Khung cấu trúc sẵn sàng phân tích cặp rổ hàng."
        };
    }

    public async Task<AiDraftImportOrder> GenerateDraftImportOrderAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        // Khung cấu trúc xử lý:
        // 1. Lọc các sản phẩm thuộc supplierId đang ở mức báo động Out of stock
        // 2. Tính số lượng cần nhập (EOQ) dựa trên tốc độ tiêu thụ
        // 3. Trả về cấu trúc phiếu nhập nháp để quản lý duyệt 1-click
        await Task.CompletedTask;
        return new AiDraftImportOrder
        {
            SuggestedSupplierId = supplierId,
            OrderNote = "Đơn nhập hàng nháp sinh tự động từ AI"
        };
    }
}
