using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs;

namespace SmartSupermarket.Backend.Features.AI.Services;

/// <summary>
/// Giao diện dịch vụ phân tích AI chuyên sâu cho chuỗi siêu thị bán lẻ.
/// </summary>
public interface IAiRetailService
{
    /// <summary>
    /// Quét toàn bộ kho hàng, phát hiện các sản phẩm có nguy cơ: cận date, cháy hàng, bán chậm.
    /// Trả về phân tích đa chiều và mức chiết khấu đề xuất từ AI.
    /// </summary>
    Task<AiProductRiskReport> AnalyzeProductRisksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phân tích lịch sử các đơn hàng để tìm ra các cặp sản phẩm khách hàng hay mua cùng nhau (Market Basket Analysis).
    /// </summary>
    Task<AiMarketBasketReport> AnalyzeMarketBasketAsync(int daysBack = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tự động sinh phiếu nhập kho nháp (Draft Import Receipt) từ các sản phẩm được AI đánh giá sắp hết hàng.
    /// </summary>
    Task<AiDraftImportOrder> GenerateDraftImportOrderAsync(int supplierId, CancellationToken cancellationToken = default);
}
