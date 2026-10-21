using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs;

namespace SmartSupermarket.Backend.Features.AI.Services;

/// <summary>
/// Giao diện dịch vụ AI Đi chợ thông minh & Lên thực đơn theo ngân sách cho khách hàng Web.
/// </summary>
public interface IAiShoppingConciergeService
{
    /// <summary>
    /// Tiếp nhận câu hỏi tự nhiên của khách (món ăn, số người, ngân sách).
    /// Đối soát kho hàng thực tế của siêu thị (ưu tiên hàng tươi sống/cận date đang giảm giá).
    /// Trả về mâm nguyên liệu chuẩn khít ngân sách kèm 1-click đưa vào giỏ hàng.
    /// </summary>
    Task<MealPlanChatResponse> PlanMealAndShopAsync(MealPlanChatRequest request, CancellationToken cancellationToken = default);
}
