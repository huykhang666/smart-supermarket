using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.Inventory.DTOs;

namespace SmartSupermarket.Backend.Features.Inventory.Services;

/// <summary>
/// Giao diện xử lý quét tự động hạn sử dụng & chiến lược giá động (Dynamic Pricing & Shelf-life Engine).
/// </summary>
public interface IShelfLifeProcessor
{
    /// <summary>
    /// Thực hiện quét hạn sử dụng:
    /// - Hàng <= 3 ngày: Tự động giảm giá 30%
    /// - Hàng <= 1 ngày: Tự động giảm giá 50%
    /// - Hàng đã quá hạn: Khóa bán (Lock sales), đánh dấu Expired, sẵn sàng hủy kho
    /// - Hàng tươi sống sau 18h: Tự động kích hoạt Flash Sale chiều tối (giảm 20%)
    /// </summary>
    Task<ShelfLifeScanResultDto> ExecuteShelfLifeAndPricingScanAsync(bool isEveningFlashSale = false, CancellationToken cancellationToken = default);
}
