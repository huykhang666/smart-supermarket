using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Services;

/// <summary>
/// Triển khai dịch vụ Trợ lý Đi chợ & Tư vấn Bữa ăn Web AI.
/// Kết nối LLM (Groq/Gemini) với dữ liệu tồn kho thực tế của siêu thị.
/// </summary>
public class AiShoppingConciergeService : IAiShoppingConciergeService
{
    private readonly AppDbContext _dbContext;
    private readonly ILlmClient _llmClient;

    public AiShoppingConciergeService(AppDbContext dbContext, ILlmClient llmClient)
    {
        _dbContext = dbContext;
        _llmClient = llmClient;
    }

    public async Task<MealPlanChatResponse> PlanMealAndShopAsync(MealPlanChatRequest request, CancellationToken cancellationToken = default)
    {
        // Khung sườn sẵn sàng:
        // 1. Phân tích ngữ nghĩa yêu cầu của khách từ request.UserPrompt (món gì, mấy người, ngân sách).
        // 2. Tra cứu cơ sở dữ liệu sản phẩm trong _dbContext (ưu tiên các mặt hàng đang giảm giá cận date / flash sale).
        // 3. Sử dụng _llmClient để gợi ý công thức và chốt giỏ hàng vừa khít ngân sách.
        await Task.CompletedTask;

        return new MealPlanChatResponse
        {
            AssistantMessage = $"Chào bạn! Tôi đã tiếp nhận yêu cầu: \"{request.UserPrompt}\". Khung dịch vụ sẵn sàng kết nối dữ liệu kho hàng.",
            DishName = "Gợi ý bữa ăn thông minh",
            Servings = request.NumberOfPeople ?? 4,
            TotalEstimatedCost = 0,
            IsWithinBudget = true
        };
    }
}
