using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.AI.DTOs;

namespace SmartSupermarket.Backend.Features.AI.Services;

public interface IAiRetailService
{
    // Quét toàn bộ kho hàng, phát hiện các sản phẩm có nguy cơ: cận date, cháy hàng, bán chậm
    Task<AiProductRiskReport> AnalyzeProductRisksAsync(CancellationToken cancellationToken = default);

    // Phân tích lịch sử các đơn hàng để tìm ra các cặp sản phẩm hay mua cùng nhau (Market Basket Analysis)
    Task<AiMarketBasketReport> AnalyzeMarketBasketAsync(int daysBack = 30, CancellationToken cancellationToken = default);

    // Tự động sinh phiếu nhập kho nháp (Draft Import Receipt) từ các sản phẩm được AI đánh giá sắp hết hàng
    Task<AiDraftImportOrder> GenerateDraftImportOrderAsync(int supplierId, CancellationToken cancellationToken = default);

    // Lưu phiếu nhập kho nháp vào cơ sở dữ liệu
    Task<ImportReceipt> CommitDraftImportOrderAsync(AiDraftImportOrder draftOrder, CancellationToken cancellationToken = default);
}
