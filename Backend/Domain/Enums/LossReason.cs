namespace SmartSupermarket.Backend.Domain.Enums;

/// <summary>
/// Phân loại nguyên nhân hao hụt khi kiểm kê kho (Shrinkage / Loss Reason)
/// Dùng để đối soát và cân bằng kho siêu thị theo quy tắc quản lý thực tế.
/// </summary>
public enum LossReason : byte
{
    None = 0,               // Không có hao hụt (Khớp tồn)
    Damaged = 1,            // Hàng dập nát (hoa quả, rau củ, đồ hộp móp méo)
    Expired = 2,            // Hết date tiêu hủy (quá hạn sử dụng)
    Broken = 3,             // Vỡ hỏng trong siêu thị (khách/nhân viên làm rơi vỡ)
    UnexplainedLoss = 4,    // Thất thoát chưa rõ nguyên nhân (mất trộm, sai sót quét POS)
    Surplus = 5,            // Thừa hàng (nhập sót phiếu, nhầm mã)
    Other = 6               // Lý do khác
}

public static class LossReasonExtensions
{
    public static string ToDisplayName(this LossReason reason)
    {
        return reason switch
        {
            LossReason.Damaged => "Hàng dập nát",
            LossReason.Expired => "Hết date tiêu hủy",
            LossReason.Broken => "Vỡ hỏng trong siêu thị",
            LossReason.UnexplainedLoss => "Thất thoát chưa rõ nguyên nhân",
            LossReason.Surplus => "Thừa hàng (nhập sót/nhầm mã)",
            LossReason.Other => "Lý do khác",
            _ => "Khớp tồn"
        };
    }
}
