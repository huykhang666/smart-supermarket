using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Domain.Entities;

namespace SmartSupermarket.Backend.Features.Inventory.Repositories;

/// <summary>
/// Hợp đồng truy xuất CSDL chuyên biệt cho nghiệp vụ Hạn sử dụng & Tồn kho
/// </summary>
public interface IShelfLifeRepository
{
    /// <summary>
    /// Lấy danh sách các lô hàng cận hạn (Quantity > 0 và ExpiryDate <= thresholdDate)
    /// </summary>
    Task<List<InventoryBatch>> GetBatchesNearExpiryAsync(DateTime thresholdDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra xem sản phẩm có còn lô nào khác hợp lệ (còn hàng và chưa hết hạn) hay không
    /// </summary>
    Task<bool> HasOtherValidBatchesAsync(int productId, int currentBatchId, DateTime today, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách sản phẩm tươi sống (Rau, Thịt, Bánh) đang Active phục vụ Flash Sale
    /// </summary>
    Task<List<Product>> GetFreshProductsForFlashSaleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi nhật ký biến động kho (Xuất hủy hàng hết hạn)
    /// </summary>
    Task AddStockHistoryAsync(StockHistory stockHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu toàn bộ thay đổi xuống CSDL
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
