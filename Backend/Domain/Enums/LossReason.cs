namespace SmartSupermarket.Backend.Domain.Enums;

public enum LossReason : byte
{
    None = 0,               
    Damaged = 1,           
    Expired = 2,            
    Broken = 3,             
    UnexplainedLoss = 4,   
    Surplus = 5,           
    Other = 6             
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
