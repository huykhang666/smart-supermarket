namespace SmartSupermarket.Backend.Features.Inventory.DTOs.Requests;

/// <summary>
/// DTO Yêu cầu quét hạn sử dụng & điều chỉnh giá tự động
/// </summary>
public class ShelfLifeScanRequest
{
    /// <summary>
    /// Kích hoạt chế độ Flash Sale chiều tối xả hàng tươi sống, bánh mì, rau củ (sau 18h)
    /// </summary>
    public bool IsEveningFlashSale { get; set; } = false;

    /// <summary>
    /// ID chi nhánh cần quét (mặc định 1)
    /// </summary>
    public int BranchId { get; set; } = 1;

    /// <summary>
    /// Nếu true: Chỉ quét mô phỏng xem trước kết quả, KHÔNG lưu thay đổi vào CSDL
    /// </summary>
    public bool DryRun { get; set; } = false;
}
