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

        if (IsMealPlanning(lower) || intent.Servings.HasValue || intent.TargetBudget.HasValue)
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
               text.Contains("mâm cơm") ||
               text.Contains("bữa tối") ||
               text.Contains("bữa trưa") ||
               text.Contains("bữa sáng") ||
               text.Contains("lẩu") ||
               text.Contains("tiệc");
    }

    private static bool IsProductSearch(string text)
    {
        return text.Contains("tìm") ||
               text.Contains("có bán") ||
               text.Contains("giá bao nhiêu") ||
               text.Contains("còn hàng");
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
