using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.Inventory.DTOs;

/// <summary>
/// Kết quả sau khi chạy quét hạn sử dụng và cập nhật giá tự động (Shelf-life & Dynamic Pricing Scan).
/// </summary>
public class ShelfLifeScanResultDto
{
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public int TotalProductsScanned { get; set; }
    
    /// <summary>
    /// Số sản phẩm còn <= 3 ngày: áp dụng giảm giá 30%
    /// </summary>
    public int Discounted30PercentCount { get; set; }

    /// <summary>
    /// Số sản phẩm còn <= 1 ngày: áp dụng giảm giá 50%
    /// </summary>
    public int Discounted50PercentCount { get; set; }

    /// <summary>
    /// Số sản phẩm đã quá hạn hôm nay: chuyển trạng thái Expired, khóa bán, lập phiếu xuất hủy
    /// </summary>
    public int ExpiredAndLockedCount { get; set; }

    /// <summary>
    /// Số sản phẩm tươi sống kích hoạt Flash Sale chiều tối (sau 18h)
    /// </summary>
    public int FlashSaleActivatedCount { get; set; }

    /// <summary>
    /// Danh sách chi tiết các mặt hàng bị tác động
    /// </summary>
    public List<ShelfLifeItemDetailDto> AffectedItems { get; set; } = new();

    public string Message { get; set; } = string.Empty;
}

public class ShelfLifeItemDetailDto
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysRemaining { get; set; }
    public string ActionTaken { get; set; } = string.Empty; // "DISCOUNT_30", "DISCOUNT_50", "LOCK_EXPIRED", "FLASH_SALE_EVENING"
}
