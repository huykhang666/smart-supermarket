using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.AI.DTOs;

#region DTOs cho Trợ lý Đi chợ & Tư vấn Bữa ăn Web AI (AI Shopping Concierge)
public class MealPlanChatRequest
{
    /// <summary>
    /// Câu hỏi / Yêu cầu tự nhiên của khách: Vd: "Tối nay nấu lẩu Thái cho 4 người tầm 350k có gì ngon không?"
    /// </summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Ngân sách tối đa của khách (nếu có xác định)
    /// </summary>
    public decimal? TargetBudget { get; set; }

    /// <summary>
    /// Số lượng người ăn (nếu có xác định)
    /// </summary>
    public int? NumberOfPeople { get; set; }

    /// <summary>
    /// Ưu tiên chọn hàng cận date / flash sale đang giảm giá để tiết kiệm chi phí
    /// </summary>
    public bool PrioritizePromotions { get; set; } = true;
}

public class MealPlanChatResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string DishName { get; set; } = string.Empty;
    public int Servings { get; set; } = 4;
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalSavedAmount { get; set; }
    public bool IsWithinBudget { get; set; } = true;

    /// <summary>
    /// Danh sách chi tiết nguyên liệu có sẵn trong kho siêu thị
    /// </summary>
    public List<MealIngredientItemDto> Ingredients { get; set; } = new();

    /// <summary>
    /// Hướng dẫn chế biến nhanh / mẹo nấu ăn từ AI
    /// </summary>
    public string CookingTips { get; set; } = string.Empty;
}

public class MealIngredientItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int QuantityToBuy { get; set; } = 1;
    public decimal RegularPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsPromotionOrNearExpiry { get; set; }
    public string PromotionNote { get; set; } = string.Empty; // Vd: "Đang giảm 30% hôm nay"
    public int InStockQuantity { get; set; }
}
#endregion
