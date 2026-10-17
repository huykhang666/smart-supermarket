using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.Inventory.DTOs.Responses;

/// <summary>
/// DTO Phản hồi kết quả quét hạn sử dụng & điều chỉnh giá tự động
/// </summary>
public class ShelfLifeScanResponse
{
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public int TotalProductsScanned { get; set; }
    
    // Thống kê phân loại các nhóm
    public int ExpiredAndLockedCount { get; set; }      // Hết hạn: Khóa bán (Inactive)
    public int Discounted50PercentCount { get; set; }   // Còn <= 1 ngày: Giảm 50%
    public int Discounted30PercentCount { get; set; }   // Còn <= 3 ngày: Giảm 30%
    public int FlashSaleActivatedCount { get; set; }    // Tươi sống chiều tối: Giảm 20%

    public bool IsDryRun { get; set; }
    public string Message { get; set; } = string.Empty;

    // Chi tiết từng sản phẩm bị tác động
    public List<ShelfLifeItemDetailResponse> AffectedItems { get; set; } = new();
}

public class ShelfLifeItemDetailResponse
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    // Các trường chi tiết theo lô hàng
    public string BatchCode { get; set; } = string.Empty;         // Mã lô (vd: LOT-01)
    public int AffectedQuantity { get; set; }                    // Số lượng sản phẩm cần dán tem
    public string Unit { get; set; } = "Hộp";                     // Đơn vị tính: Hộp, Chai, Bịch
    public DateTime? ManufacturingDate { get; set; }              // Ngày sản xuất / Ngày nhập
    public DateTime? ExpiryDate { get; set; }                     // Ngày hết hạn
    public int? DaysRemaining { get; set; }                       // Số ngày còn lại
    public string StorageLocation { get; set; } = string.Empty;   // Vị trí lưu kho: "Kệ A1", "Tủ mát 2"

    public decimal OriginalPrice { get; set; }                   // Giá niêm yết gốc
    public decimal NewPrice { get; set; }                        // Giá sau khi giảm
    public decimal DiscountPercent { get; set; }                 // % Giảm giá
    public string ActionTaken { get; set; } = string.Empty;      // "LOCK_EXPIRED", "DISCOUNT_50", "DISCOUNT_30", "FLASH_SALE_EVENING"
}
