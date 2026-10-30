using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Services.Intent;

public class IntentAnalyzer : IIntentAnalyzer
{
    private static readonly Regex ServingsRegex = new(
        @"(?:cho\s+)?(\d+)\s*(?:người|khách|suất|phần|khẩu\s*phần)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BudgetRegexK = new(
        @"(\d+(?:[.,]\d+)?)\s*(?:k|nghìn|ngàn|tr|triệu)(?:\s*(?:đồng|đ|vnd))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BudgetRegexFull = new(
        @"(\d{2,3}(?:[.,]\d{3})+)\s*(?:đồng|đ|vnd)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<UserIntentDto> AnalyzeIntentAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var cleaned = prompt?.Trim() ?? string.Empty;
        var lower = cleaned.ToLowerInvariant();

        var intent = new UserIntentDto
        {
            RawPrompt = cleaned,
            Confidence = 0.9,
            Servings = ExtractServings(lower),
            TargetBudget = ExtractBudget(lower)
        };

        if (IsGreeting(lower))
        {
            intent.Type = IntentType.Greeting;
            return Task.FromResult(intent);
        }

        if (IsCartAction(lower))
        {
            intent.Type = IntentType.CartAction;
            return Task.FromResult(intent);
        }

        if (IsPromotionInquiry(lower))
        {
            intent.Type = IntentType.PromotionInquiry;
            return Task.FromResult(intent);
        }

        if (IsCulinaryAdvice(lower))
        {
            intent.Type = IntentType.CulinaryAdvice;
            return Task.FromResult(intent);
        }

        if (IsRetailShopping(lower) && !IsExplicitMealTheme(lower))
        {
            ClassifyRetailIntent(lower, intent);
            return Task.FromResult(intent);
        }

        if (IsMealPlanning(lower) || intent.Servings.HasValue)
        {
            intent.Type = IntentType.MealPlanning;
            ExtractDishTheme(lower, intent);
            return Task.FromResult(intent);
        }

        if (IsProductSearch(lower))
        {
            intent.Type = IntentType.ProductSearch;
            return Task.FromResult(intent);
        }

        intent.Type = IntentType.GeneralChat;
        return Task.FromResult(intent);
    }

    private static bool IsGreeting(string text)
    {
        return text is "hi" or "hello" or "alo" or "xin chào" ||
               text.StartsWith("chào") ||
               text.StartsWith("hello") ||
               text.StartsWith("hi ");
    }

    private static bool IsCartAction(string text)
    {
        return text.Contains("thêm vào giỏ") ||
               text.Contains("nạp giỏ") ||
               text.Contains("chọn mâm") ||
               text.Contains("lấy toàn bộ") ||
               text.Contains("mua mâm này") ||
               text.Contains("bỏ vào giỏ");
    }

    private static bool IsPromotionInquiry(string text)
    {
        return text.Contains("giảm giá") ||
               text.Contains("khuyến mãi") ||
               text.Contains("flash sale") ||
               text.Contains("voucher") ||
               text.Contains("ưu đãi") ||
               text.Contains("cận date") ||
               text.Contains("xả hàng");
    }

    private static bool IsCulinaryAdvice(string text)
    {
        return (text.Contains("cách nấu") || text.Contains("mẹo") || text.Contains("bí quyết") ||
                text.Contains("luộc sao") || text.Contains("ướp sao") || text.Contains("khử tanh")) &&
               !text.Contains("mua gì") && !text.Contains("nấu món gì");
    }

    private static bool IsMealPlanning(string text)
    {
        return text.Contains("ăn gì") ||
               text.Contains("nấu gì") ||
               text.Contains("món gì") ||
               text.Contains("nấu ăn") ||
               text.Contains("mâm cơm") ||
               text.Contains("bữa cơm") ||
               text.Contains("bữa tối") ||
               text.Contains("bữa trưa") ||
               text.Contains("bữa sáng") ||
               text.Contains("thực đơn") ||
               text.Contains("công thức") ||
               text.Contains("nguyên liệu nấu") ||
               text.Contains("lẩu") ||
               text.Contains("kho tàu") ||
               text.Contains("canh chua") ||
               text.Contains("tiệc");
    }

    private static bool IsProductSearch(string text)
    {
        return text.Contains("tìm") ||
               text.Contains("có bán") ||
               text.Contains("giá bao nhiêu") ||
               text.Contains("còn hàng");
    }

    private static bool IsExplicitMealTheme(string text)
    {
        return text.Contains("cơm tấm") || text.Contains("lẩu") || text.Contains("kho tàu") ||
               text.Contains("canh chua") || text.Contains("bữa tối") || text.Contains("bữa trưa") ||
               text.Contains("bữa sáng") || text.Contains("mâm cơm") || text.Contains("bữa cơm") ||
               text.Contains("nấu ăn") || text.Contains("nấu món") || text.Contains("nguyên liệu nấu") ||
               text.Contains("công thức nấu") || text.Contains("thực đơn") || text.Contains("người ăn") ||
               text.Contains("món gì") || text.Contains("nấu gì") || text.Contains("ăn gì");
    }

    private static bool IsRetailShopping(string text)
    {
        return text.Contains("dầu gội") ||
               text.Contains("sữa tắm") ||
               text.Contains("rửa chén") ||
               text.Contains("nước rửa chén") ||
               text.Contains("nồi cơm") ||
               text.Contains("nồi") ||
               text.Contains("chảo") ||
               text.Contains("bếp") ||
               text.Contains("nồi nhôm") ||
               text.Contains("giấy vệ sinh") ||
               text.Contains("khăn giấy") ||
               text.Contains("em bé") ||
               text.Contains("đồ cho em bé") ||
               text.Contains("nước giặt") ||
               text.Contains("nước xả") ||
               text.Contains("hộp thủy tinh") ||
               text.Contains("hộp nhựa") ||
               text.Contains("bông tẩy trang") ||
               text.Contains("dao cạo") ||
               text.Contains("kem đánh răng") ||
               text.Contains("bàn chải") ||
               text.Contains("bột giặt") ||
               text.Contains("lau sàn") ||
               text.Contains("tã") ||
               text.Contains("bỉm") ||
               text.Contains("bình giữ nhiệt") ||
               text.Contains("màng bọc") ||
               text.Contains("túi rác") ||
               text.Contains("rẻ mà ổn") ||
               text.Contains("dùng lâu, giá hợp lý") ||
               text.Contains("dùng lâu") ||
               text.Contains("tiết kiệm mà tốt") ||
               text.Contains("đáng tiền") ||
               text.Contains("đáng mua") ||
               text.Contains("phân khúc nào") ||
               text.Contains("loại rẻ, loại tiêu chuẩn hay loại cao cấp");
    }

    private static void ClassifyRetailIntent(string text, UserIntentDto intent)
    {
        if (text.Contains("so sánh") || text.Contains("phân khúc nào") || text.Contains("loại rẻ, loại tiêu chuẩn hay loại cao cấp") || text.Contains("khác nhau thế nào"))
        {
            intent.Type = IntentType.CategoryComparison;
        }
        else if (text.Contains("giá bao nhiêu") || text.Contains("so sánh giá") || text.Contains("bảng giá"))
        {
            intent.Type = IntentType.PriceComparison;
        }
        else if (text.Contains("tốt nhất") || text.Contains("chất lượng") || text.Contains("loại nào tốt"))
        {
            intent.Type = IntentType.QualityComparison;
        }
        else if (intent.TargetBudget.HasValue || text.Contains("ngân sách") || text.Contains("tầm giá"))
        {
            intent.Type = IntentType.BudgetShopping;
        }
        else if (text.Contains("tiết kiệm mà tốt") || text.Contains("đáng tiền") || text.Contains("đáng mua") || text.Contains("hợp lý") || text.Contains("p/p"))
        {
            intent.Type = IntentType.ValueForMoneyShopping;
            intent.PreferredTier = "Tier 2: Đáng tiền nhất";
        }
        else if (text.Contains("dùng lâu") || text.Contains("bền") || text.Contains("bền bỉ"))
        {
            intent.Type = IntentType.DurableProductSearch;
            intent.PreferredTier = "Tier 3: Bền bỉ";
        }
        else if (text.Contains("rẻ mà ổn") || text.Contains("rẻ nhất") || text.Contains("giá rẻ"))
        {
            intent.Type = IntentType.CheapProductSearch;
            intent.PreferredTier = "Tier 1: Tiết kiệm";
        }
        else if (text.Contains("cao cấp") || text.Contains("xịn") || text.Contains("hàng hiệu"))
        {
            intent.Type = IntentType.PremiumProductSearch;
            intent.PreferredTier = "Tier 4: Cao cấp";
        }
        else if (text.Contains("khuyến mãi") || text.Contains("flash sale") || text.Contains("giảm giá") || text.Contains("voucher"))
        {
            intent.Type = IntentType.PromotionAwareShopping;
        }
        else if (text.Contains("còn hàng") || text.Contains("sẵn hàng") || text.Contains("tồn kho"))
        {
            intent.Type = IntentType.InventoryAwareShopping;
        }
        else
        {
            intent.Type = IntentType.ProductRecommendation;
        }

        if (text.Contains("dầu gội") || text.Contains("sữa tắm") || text.Contains("gội") || text.Contains("tắm") || text.Contains("dao cạo") || text.Contains("kem đánh răng") || text.Contains("bàn chải"))
            intent.RetailCategory = "Chăm sóc cá nhân";
        else if (text.Contains("rửa chén") || text.Contains("nước giặt") || text.Contains("nước xả") || text.Contains("lau sàn") || text.Contains("hóa phẩm"))
            intent.RetailCategory = "Hóa phẩm & Giặt xả";
        else if (text.Contains("nồi") || text.Contains("chảo") || text.Contains("nhà bếp") || text.Contains("bếp") || text.Contains("hộp thủy tinh") || text.Contains("hộp nhựa"))
            intent.RetailCategory = "Đồ dùng gia đình & Nhà bếp";
        else if (text.Contains("giấy") || text.Contains("khăn"))
            intent.RetailCategory = "Giấy vệ sinh & Khăn giấy";
        else if (text.Contains("em bé") || text.Contains("bé") || text.Contains("tã") || text.Contains("bỉm"))
            intent.RetailCategory = "Mẹ & Bé";
    }

    private static int? ExtractServings(string text)
    {
        var match = ServingsRegex.Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int servings) && servings > 0 && servings <= 50)
        {
            return servings;
        }

        if (text.Contains("1 người") || text.Contains("một người") || text.Contains("mình tôi") || text.Contains("cho tôi"))
        {
            return 1;
        }

        return null;
    }

    private static decimal? ExtractBudget(string text)
    {
        var fullMatch = BudgetRegexFull.Match(text);
        if (fullMatch.Success)
        {
            var raw = fullMatch.Groups[1].Value.Replace(".", "").Replace(",", "");
            if (decimal.TryParse(raw, out decimal val) && val > 0)
            {
                return val;
            }
        }

        var kMatch = BudgetRegexK.Match(text);
        if (kMatch.Success)
        {
            var rawVal = kMatch.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(rawVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal num))
            {
                var unit = kMatch.Value.ToLowerInvariant();
                if (unit.Contains("tr") || unit.Contains("triệu"))
                {
                    return num * 1_000_000m;
                }
                return num * 1_000m;
            }
        }

        return null;
    }

    private static void ExtractDishTheme(string text, UserIntentDto intent)
    {
        if (text.Contains("lẩu bò")) intent.DishTheme = "Lẩu bò nấm";
        else if (text.Contains("lẩu thái")) intent.DishTheme = "Lẩu Thái hải sản";
        else if (text.Contains("lẩu")) intent.DishTheme = "Lẩu";
        else if (text.Contains("thịt kho") || text.Contains("kho tàu")) intent.DishTheme = "Thịt kho tàu";
        else if (text.Contains("canh chua")) intent.DishTheme = "Canh chua";
        else if (text.Contains("chay")) intent.DishTheme = "Cơm chay";
        else if (text.Contains("nướng") || text.Contains("bbq")) intent.DishTheme = "Tiệc nướng BBQ";
    }
}
