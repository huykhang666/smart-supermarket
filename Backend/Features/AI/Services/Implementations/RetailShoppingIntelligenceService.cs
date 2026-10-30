using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Services.Interfaces;
using SmartSupermarket.Backend.Features.AI.Tools;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Services.Implementations;

public class RetailShoppingIntelligenceService : IRetailShoppingIntelligenceService
{
    private readonly AppDbContext _dbContext;
    private readonly IRetailTool _retailTool;
    private readonly ILlmClient _llmClient;

    public RetailShoppingIntelligenceService(
        AppDbContext dbContext,
        IRetailTool retailTool,
        ILlmClient llmClient)
    {
        _dbContext = dbContext;
        _retailTool = retailTool;
        _llmClient = llmClient;
    }

    public async Task<RetailShoppingResultDto> AdviseRetailShoppingAsync(
        string prompt,
        decimal? targetBudget = null,
        string? preferredTier = null,
        CancellationToken cancellationToken = default)
    {
        var category = DetectRetailCategory(prompt);
        var intentTrait = DetectIntentTrait(prompt);

        if (targetBudget == null || targetBudget <= 0)
        {
            var budgetMatch = Regex.Match(prompt, @"(\d+)\s*[kK]");
            if (budgetMatch.Success && decimal.TryParse(budgetMatch.Groups[1].Value, out decimal parsedK))
            {
                targetBudget = parsedK * 1000m;
            }
        }

        RetailTier? filterTier = null;
        if (!string.IsNullOrEmpty(preferredTier))
        {
            if (preferredTier.Contains("1") || preferredTier.ToLower().Contains("rẻ") || preferredTier.ToLower().Contains("tiết kiệm"))
                filterTier = RetailTier.Tier1CheapestAcceptable;
            else if (preferredTier.Contains("2") || preferredTier.ToLower().Contains("đáng tiền") || preferredTier.ToLower().Contains("tiêu chuẩn"))
                filterTier = RetailTier.Tier2BalancedValue;
            else if (preferredTier.Contains("3") || preferredTier.ToLower().Contains("bền") || preferredTier.ToLower().Contains("dùng lâu"))
                filterTier = RetailTier.Tier3DurableHighPerformance;
            else if (preferredTier.Contains("4") || preferredTier.ToLower().Contains("cao cấp"))
                filterTier = RetailTier.Tier4PremiumLuxury;
        }
        else
        {
            if (intentTrait.Contains("rẻ"))
                filterTier = RetailTier.Tier1CheapestAcceptable;
            else if (intentTrait.Contains("bền"))
                filterTier = RetailTier.Tier3DurableHighPerformance;
            else if (intentTrait.Contains("Best Value") || intentTrait.Contains("tiết kiệm mà tốt") || intentTrait.Contains("đáng tiền"))
                filterTier = RetailTier.Tier2BalancedValue;
        }

        var recommendations = await GetRecommendationsByCategoryAsync(category, targetBudget, filterTier, cancellationToken);
        var comparisonTable = await _retailTool.GenerateCategoryComparisonAsync(category, cancellationToken);

        // Build Assistant XAI Explanation
        string summaryMessage = BuildExplainableResponse(prompt, category, intentTrait, recommendations, comparisonTable);

        return new RetailShoppingResultDto
        {
            AssistantMessage = summaryMessage,
            ShoppingCategory = category,
            UseCase = intentTrait,
            Recommendations = recommendations,
            ComparisonTable = comparisonTable
        };
    }

    public async Task<List<RetailProductRecommendationDto>> GetRecommendationsByCategoryAsync(
        string category,
        decimal? targetBudget = null,
        RetailTier? preferredTier = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch products from DB
        var query = _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active);

        var lowerCat = category.ToLower();
        if (lowerCat.Contains("dầu gội") || lowerCat.Contains("gội"))
        {
            query = query.Where(p => p.ProductName.ToLower().Contains("dầu gội") ||
                                     p.ProductName.ToLower().Contains("pantene") ||
                                     p.ProductName.ToLower().Contains("clear"));
        }
        else if (lowerCat.Contains("nước rửa chén") || lowerCat.Contains("rửa chén"))
        {
            query = query.Where(p => p.ProductName.ToLower().Contains("rửa chén") ||
                                     p.CategoryId == 7);
        }
        else if (lowerCat.Contains("nồi") || lowerCat.Contains("chảo") || lowerCat.Contains("nhà bếp"))
        {
            query = query.Where(p => p.ProductName.ToLower().Contains("chảo") ||
                                     p.ProductName.ToLower().Contains("nồi") ||
                                     p.ProductName.ToLower().Contains("lock&lock") ||
                                     p.CategoryId == 6);
        }
        else if (lowerCat.Contains("giấy") || lowerCat.Contains("khăn"))
        {
            query = query.Where(p => p.ProductName.ToLower().Contains("giấy") ||
                                     p.ProductName.ToLower().Contains("bông tẩy trang") ||
                                     p.ProductName.ToLower().Contains("khăn"));
        }
        else if (lowerCat.Contains("em bé") || lowerCat.Contains("bé"))
        {
            query = query.Where(p => p.ProductName.ToLower().Contains("bobby") ||
                                     p.ProductName.ToLower().Contains("bé") ||
                                     p.ProductName.ToLower().Contains("khăn ướt") ||
                                     p.ProductName.ToLower().Contains("sữa"));
        }
        else
        {
            query = query.Where(p => p.Category != null && p.Category.CategoryName.ToLower().Contains(lowerCat));
        }

        var products = await query.ToListAsync(cancellationToken);

        // Fetch batches to check live stock
        var productIds = products.Select(p => p.ProductId).ToList();
        var batches = await _dbContext.InventoryBatches
            .Where(b => productIds.Contains(b.ProductId) && b.Quantity > 0)
            .ToListAsync(cancellationToken);

        var stockLookup = batches
            .GroupBy(b => b.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        // Classify into Tiers and calculate Unit Metrics
        var list = new List<RetailProductRecommendationDto>();

        foreach (var p in products)
        {
            var regularPrice = p.Price;
            var finalPrice = p.Price;
            var discountPercent = 0m;
            var inStock = stockLookup.TryGetValue(p.ProductId, out var qty) ? qty : 10;

            var (tier, tierLabel, unitMetric, pros, cons, isChoice, highlight) = EvaluateProductTierAndMetric(p, finalPrice);

            var rec = new RetailProductRecommendationDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Barcode = p.Barcode,
                ImageUrl = p.ImageUrl ?? string.Empty,
                Unit = p.Unit ?? string.Empty,
                RegularPrice = regularPrice,
                FinalPrice = finalPrice,
                DiscountPercent = discountPercent,
                InStockQuantity = inStock,
                Tier = tier,
                TierLabel = tierLabel,
                PricePerUnitMetric = unitMetric,
                Pros = pros,
                Cons = cons,
                IsRecommendedChoice = isChoice,
                HighlightReason = highlight,
                IsWithinBudget = targetBudget == null || finalPrice <= targetBudget.Value,
                CategoryName = p.Category?.CategoryName ?? category
            };

            list.Add(rec);
        }

        // Apply filters & rank
        if (preferredTier.HasValue)
        {
            list = list.OrderByDescending(r => r.IsWithinBudget)
                       .ThenByDescending(r => r.Tier == preferredTier.Value)
                       .ThenBy(r => r.FinalPrice)
                       .ToList();
        }
        else
        {
            // Rank: Within budget first -> Tier 2 (Best value) / Recommended -> Tier 1 -> Tier 3/4 -> Price ascending
            list = list.OrderByDescending(r => r.IsWithinBudget)
                       .ThenByDescending(r => r.IsRecommendedChoice)
                       .ThenBy(r => r.Tier == RetailTier.Tier2BalancedValue ? 0 :
                                    r.Tier == RetailTier.Tier1CheapestAcceptable ? 1 : 2)
                       .ThenBy(r => r.FinalPrice)
                       .ToList();
        }

        return list.Take(5).ToList();
    }

    public Task<RetailComparisonTableDto> GenerateComparisonTableAsync(string category, CancellationToken cancellationToken = default)
    {
        return _retailTool.GenerateCategoryComparisonAsync(category, cancellationToken);
    }

    private static string DetectRetailCategory(string prompt)
    {
        var lower = prompt.ToLowerInvariant();
        if (lower.Contains("dầu gội") || lower.Contains("sữa tắm") || lower.Contains("gội") || lower.Contains("tắm") || lower.Contains("dao cạo") || lower.Contains("kem đánh răng"))
            return "Chăm sóc cá nhân (Dầu gội & Sữa tắm)";
        if (lower.Contains("rửa chén") || lower.Contains("nước rửa chén") || lower.Contains("nước giặt") || lower.Contains("nước xả") || lower.Contains("lau sàn") || lower.Contains("hóa phẩm"))
            return "Hóa phẩm & Giặt xả (Nước rửa chén & Nước giặt)";
        if (lower.Contains("nồi") || lower.Contains("chảo") || lower.Contains("bếp") || lower.Contains("nồi cơm") || lower.Contains("hộp thủy tinh") || lower.Contains("màng bọc"))
            return "Đồ dùng gia đình & Nhà bếp (Nồi chảo & Gia dụng)";
        if (lower.Contains("giấy") || lower.Contains("giấy vệ sinh") || lower.Contains("khăn giấy") || lower.Contains("bông tẩy trang"))
            return "Giấy vệ sinh & Khăn giấy gia đình";
        if (lower.Contains("em bé") || lower.Contains("bé") || lower.Contains("trẻ em") || lower.Contains("sơ sinh") || lower.Contains("tã") || lower.Contains("bỉm"))
            return "Sản phẩm Mẹ & Bé (Chăm sóc dịu nhẹ)";
        if (lower.Contains("nước ngọt") || lower.Contains("bia") || lower.Contains("nước giải khát") || lower.Contains("trà"))
            return "Nước giải khát & Đồ uống";
        if (lower.Contains("bánh kẹo") || lower.Contains("ăn vặt"))
            return "Bánh kẹo & Ăn vặt";
        if (lower.Contains("sữa"))
            return "Sữa & Sản phẩm từ sữa";

        return "Hàng tiêu dùng siêu thị";
    }

    private static string DetectIntentTrait(string prompt)
    {
        var lower = prompt.ToLowerInvariant();
        if (lower.Contains("rẻ mà ổn") || lower.Contains("rẻ nhất") || lower.Contains("giá rẻ"))
            return "Tìm sản phẩm giá rẻ, chất lượng ổn định (Tier 1 hoặc Tier 2)";
        if (lower.Contains("dùng lâu") || lower.Contains("bền") || lower.Contains("bền bỉ"))
            return "Tìm sản phẩm bền bỉ, sử dụng dài lâu (Tier 3 High Performance)";
        if (lower.Contains("tiết kiệm mà tốt") || lower.Contains("đáng tiền") || lower.Contains("đáng mua") || lower.Contains("hợp lý") || lower.Contains("p/p"))
            return "Tối ưu chi phí trên dung tích - Best Value (Tier 2)";
        if (lower.Contains("em bé") || lower.Contains("phân khúc nào"))
            return "Tư vấn chọn phân khúc an toàn lành tính cho gia đình";
        if (lower.Contains("so sánh") || lower.Contains("khác nhau thế nào") || lower.Contains("loại nào hơn"))
            return "So sánh toàn diện các phân khúc giá và tính năng";

        return "Tư vấn mua sắm thông minh chuẩn siêu thị";
    }

    private static (RetailTier Tier, string TierLabel, string UnitMetric, List<string> Pros, List<string> Cons, bool IsChoice, string Highlight) EvaluateProductTierAndMetric(Product p, decimal finalPrice)
    {
        var name = p.ProductName;
        var lower = name.ToLower();

        // 1. Dầu gội / Chăm sóc cá nhân
        if (lower.Contains("dầu gội") || lower.Contains("pantene") || lower.Contains("clear"))
        {
            if (lower.Contains("thảo dược") || finalPrice >= 175000)
            {
                return (RetailTier.Tier3DurableHighPerformance, "Tier 3: Nuôi dưỡng chuyên sâu",
                    $"{Math.Round(finalPrice / 630m):N0} đ/g",
                    new List<string> { "Chiết xuất thảo dược tự nhiên quý", "Nuôi dưỡng chân tóc, ngừa rụng", "Dung tích lớn 630g" },
                    new List<string> { "Giá cao hơn các dòng phổ thông" },
                    false, "Phù hợp cho tóc hư tổn hoặc da đầu nhạy cảm");
            }
            if (lower.Contains("clear men") || (finalPrice >= 160000 && finalPrice < 175000))
            {
                return (RetailTier.Tier2BalancedValue, "Tier 2: Đáng tiền nhất (Best Value)",
                    $"{Math.Round(finalPrice / 630m):N0} đ/g",
                    new List<string> { "Chai đại 630g dùng tiết kiệm 4-5 tháng", "Sạch gàu bạc hà mát lạnh tức thì", "Chi phí trên mỗi lần gội cực thấp (~1.200 đ)" },
                    new List<string> { "Kích thước chai lớn, khó mang đi du lịch" },
                    true, "Lựa chọn số 1: Vừa sạch sâu vừa cực kỳ lợi tiền trên mỗi gram!");
            }
            if (lower.Contains("pantene") || finalPrice < 160000)
            {
                return (RetailTier.Tier1CheapestAcceptable, "Tier 1: Tiết kiệm & Ổn định",
                    $"{Math.Round(finalPrice / 650m):N0} đ/ml",
                    new List<string> { "Giá êm nhất phân khúc chai lớn (159.000 đ)", "Phục hồi tóc suôn mượt nhanh", "Thương hiệu quốc tế uy tín" },
                    new List<string> { "Khả năng trị gàu không mạnh bằng dòng bạc hà" },
                    false, "Tối ưu cho người thích tóc mềm mượt với chi phí tiết kiệm");
            }
        }

        // 2. Nước rửa chén / Nước giặt / Hóa phẩm
        if (lower.Contains("rửa chén") || lower.Contains("sunlight"))
        {
            if (lower.Contains("can 3.6kg") || finalPrice >= 80000)
            {
                var metric = $"{Math.Round(finalPrice / 3.6m):N0} đ/kg";
                return (RetailTier.Tier2BalancedValue, "Tier 2: Đáng tiền nhất (Best Value)",
                    metric,
                    new List<string> { "Đơn giá chỉ ~29.167 đ/kg (rẻ hơn 25% so với mua chai nhỏ)", "Can 3.6kg dùng thoải mái cả gia đình trong 6-8 tháng", "Khử sạch dầu mỡ tanh với chiết xuất chanh tươi" },
                    new List<string> { "Can to nặng, nên chiết ra chai nhỏ để tiện rót" },
                    true, "Khuyên mua: Mua can lớn tiết kiệm ngay gần 35.000 đ so với mua lẻ từng chai!");
            }
            if (lower.Contains("chai 750g") || finalPrice < 50000)
            {
                var metric = $"{Math.Round(finalPrice / 0.75m):N0} đ/kg";
                return (RetailTier.Tier1CheapestAcceptable, "Tier 1: Tiết kiệm chi phí đầu vào",
                    metric,
                    new List<string> { "Giá mua chỉ 29.000 đ, nhẹ túi tiền", "Chai nhỏ vừa tay cầm, dễ để bên bồn rửa", "Sạch dầu mỡ nhanh" },
                    new List<string> { "Đơn giá theo kg (~38.600 đ/kg) đắt hơn mua can lớn" },
                    false, "Phù hợp phòng trọ, người độc thân ít nấu nướng");
            }
        }

        // 3. Gia dụng / Nồi cơm điện
        if (lower.Contains("nồi cơm"))
        {
            if (lower.Contains("sharp") || finalPrice >= 500000)
            {
                return (RetailTier.Tier3DurableHighPerformance, "Tier 3: Bền bỉ & Dung tích lớn",
                    "Dung tích 1.8L (Bảo hành 12 tháng Sharp)",
                    new List<string> { "Thương hiệu Sharp Nhật Bản siêu bền bỉ", "Dung tích lớn 1.8L nấu thoải mái 4-6 người ăn", "Mâm nhiệt lớn tỏa nhiệt đều, cơm dẻo không cháy" },
                    new List<string> { "Giá 690.000 đ (vượt ngân sách 500k nếu có yêu cầu)" },
                    false, "Đáng đầu tư dài lâu cho gia đình đông người (4-6 người)");
            }
            if (lower.Contains("comet") || finalPrice <= 500000)
            {
                return (RetailTier.Tier1CheapestAcceptable, "Tier 1: Tiết kiệm & Nhỏ gọn",
                    "Dung tích 0.8L (~125.000 đ/người)",
                    new List<string> { "Giá chỉ 250.000 đ (nằm trọn trong ngân sách 500k)", "Lòng nồi chống dính dễ vệ sinh, nấu nhanh chín", "Thiết kế nhỏ gọn, tiết kiệm điện năng cho 1-2 người" },
                    new List<string> { "Dung tích 0.8L chỉ phù hợp cho cá nhân hoặc 1-2 người ăn" },
                    true, "Lựa chọn số 1: Vừa túi tiền, bền và tiết kiệm điện hoàn hảo dưới 500k!");
            }
        }

        // 4. Gia dụng / Nồi / Chảo
        if (lower.Contains("chảo") || lower.Contains("nồi"))
        {
            if (lower.Contains("elmich") || finalPrice >= 200000)
            {
                return (RetailTier.Tier3DurableHighPerformance, "Tier 3: Bền bỉ & Chuẩn từ",
                    "Bảo hành 24 tháng đáy từ",
                    new List<string> { "Đáy từ bắt nhiệt siêu nhạy, dùng cho mọi loại bếp", "Hợp kim nhôm dày chống cong vênh", "Lớp chống dính cao cấp tuổi thọ 3+ năm" },
                    new List<string> { "Giá đầu tư ban đầu cao hơn nồi chảo nhôm thường" },
                    false, "Đáng đầu tư dài lâu: Không sợ phồng đáy, an toàn tuyệt đối cho sức khỏe!");
            }
            if (lower.Contains("sunhouse") && (lower.Contains("chảo") || lower.Contains("nồi")))
            {
                return (RetailTier.Tier2BalancedValue, "Tier 2: Ngon - Bổ - Rẻ hợp túi tiền",
                    "Độ bền 1.5 - 2 năm",
                    new List<string> { "Giá hợp lý (165.000 đ), bề mặt vân đá kháng trầy", "Dẫn nhiệt nhanh giúp chiên xào chín đều", "Trọng lượng vừa phải, cầm không mỏi tay" },
                    new List<string> { "Cần tránh dùng cước sắt cọ rửa để giữ lớp chống dính" },
                    true, "Cân bằng p/p hoàn hảo: Dùng tốt, bền bỉ mà giá lại rất mềm");
            }
        }

        // 5. Giấy vệ sinh & Khăn giấy
        if (lower.Contains("giấy vệ sinh") || lower.Contains("pulppy") || lower.Contains("an an"))
        {
            if (lower.Contains("pulppy") || finalPrice >= 70000)
            {
                return (RetailTier.Tier2BalancedValue, "Tier 2: Đáng tiền nhất (Best Value)",
                    $"{Math.Round(finalPrice / 10m):N0} đ/cuộn",
                    new List<string> { "Giấy 3 lớp lụa siêu mềm mịn, dai không mủn khi ướt", "Lốc 10 cuộn dùng bền bỉ 2-3 tháng", "100% bột giấy nguyên sinh an toàn tuyệt đối" },
                    new List<string> { "Giá cao hơn các dòng giấy tái chế hoặc 2 lớp phổ thông" },
                    true, "Khuyên mua: Êm ái tuyệt đối, giá trên từng cuộn cực kỳ hợp lý (~8.500 đ/cuộn)!");
            }
            if (lower.Contains("an an") || finalPrice < 70000)
            {
                return (RetailTier.Tier1CheapestAcceptable, "Tier 1: Tiết kiệm tối đa",
                    $"{Math.Round(finalPrice / 10m):N0} đ/cuộn",
                    new List<string> { "Giá chỉ 45.000 đ/lốc 10 cuộn (~4.500 đ/cuộn)", "Không lõi giúp tăng lượng giấy thực tế", "Tiết kiệm chi phí tối đa cho gia đình đông người" },
                    new List<string> { "Độ mềm mịn ở mức trung bình, không mềm bằng dòng 3 lớp" },
                    false, "Tối ưu chi phí sinh hoạt hàng tháng");
            }
        }

        // 6. Khăn ướt em bé / Mẹ & Bé
        if (lower.Contains("bobby") || lower.Contains("khăn ướt") || lower.Contains("bé"))
        {
            return (RetailTier.Tier2BalancedValue, "Tier 2: An toàn lành tính cho bé",
                $"{Math.Round(finalPrice / 80m):N0} đ/miếng",
                new List<string> { "Công thức 99% nước tinh khiết, 0% cồn, 0% paraben", "Chất vải mềm mịn dập nổi không gây kích ứng da non", "Giá siêu thị chỉ 32.000 đ cho gói 80 miếng (~400 đ/miếng)" },
                new List<string> { "Cần đóng chặt nắp decal sau khi rút để giữ độ ẩm" },
                true, "An tâm tuyệt đối: Đạt chuẩn y tế da liễu cho trẻ sơ sinh");
        }

        // Default Evaluation
        if (finalPrice <= 50000)
        {
            return (RetailTier.Tier1CheapestAcceptable, "Tier 1: Tiết kiệm",
                $"{finalPrice:N0} đ/{p.Unit ?? "món"}",
                new List<string> { "Giá hạt dẻ, phù hợp ngân sách tiết kiệm", "Chất lượng đạt chuẩn siêu thị" },
                new List<string> { "Dung tích hoặc quy cách đóng gói nhỏ" },
                false, "Lựa chọn tiết kiệm chi phí");
        }
        else if (finalPrice <= 150000)
        {
            return (RetailTier.Tier2BalancedValue, "Tier 2: Đáng tiền nhất (Best Value)",
                $"{finalPrice:N0} đ/{p.Unit ?? "món"}",
                new List<string> { "Tối ưu giữa giá trị và chi phí", "Dung tích chuẩn, được người tiêu dùng chọn nhiều nhất" },
                new List<string> { "Mức chi tiêu trung bình" },
                true, "Sự lựa chọn cân đối nhất về chất lượng và giá thành");
        }
        else
        {
            return (RetailTier.Tier3DurableHighPerformance, "Tier 3: Bền bỉ / Cao cấp",
                $"{finalPrice:N0} đ/{p.Unit ?? "món"}",
                new List<string> { "Chất liệu cao cấp, độ bền vượt trội", "Thương hiệu hàng đầu" },
                new List<string> { "Chi phí ban đầu cao hơn" },
                false, "Dành cho gia đình muốn sử dụng dài lâu");
        }
    }

    private static string BuildExplainableResponse(
        string prompt,
        string category,
        string intentTrait,
        List<RetailProductRecommendationDto> recommendations,
        RetailComparisonTableDto comparisonTable)
    {
        var sb = new System.Text.StringBuilder();

        // 1. Result First (Kết luận ngay phân khúc / sản phẩm tối ưu nhất)
        var bestChoice = recommendations.FirstOrDefault(r => r.IsRecommendedChoice) ?? recommendations.FirstOrDefault();
        if (bestChoice != null)
        {
            sb.AppendLine($"🎯 **Gợi ý tốt nhất cho bạn**: **{bestChoice.ProductName}** ({bestChoice.FinalPrice:N0} đ/{bestChoice.Unit})");
            sb.AppendLine($"• **Phân khúc**: {bestChoice.TierLabel}");
            sb.AppendLine($"• **Lý do nổi bật**: {bestChoice.HighlightReason}");
            if (!string.IsNullOrEmpty(bestChoice.PricePerUnitMetric))
            {
                sb.AppendLine($"• **Chỉ số tiết kiệm**: {bestChoice.PricePerUnitMetric}");
            }
            sb.AppendLine();
        }

        // 2. Reason Second (Phân tích chi tiết phương án và trade-off)
        sb.AppendLine($"🔍 **Phân tích so sánh các lựa chọn tại siêu thị:**");
        foreach (var item in recommendations.Take(3))
        {
            string starBadge = item.IsRecommendedChoice ? " ⭐ [KHUYÊN DÙNG]" : "";
            string budgetBadge = item.IsWithinBudget ? " [Trong ngân sách]" : " ⚠️ [Vượt ngân sách]";
            sb.AppendLine($"\n**{item.TierLabel}**{starBadge}{budgetBadge}: **{item.ProductName}** — **{item.FinalPrice:N0} đ**");
            if (item.Pros.Any())
            {
                sb.AppendLine($"  + Ưu điểm: {string.Join(", ", item.Pros)}");
            }
            if (item.Cons.Any())
            {
                sb.AppendLine($"  - Cần cân nhắc: {string.Join(", ", item.Cons)}");
            }
        }

        // 3. Trade-off Analysis & Supermarket Guidance
        sb.AppendLine("\n💡 **Lời khuyên mua sắm thông minh:**");
        var lowerPrompt = prompt.ToLowerInvariant();
        if (lowerPrompt.Contains("nồi cơm"))
        {
            sb.AppendLine("• Nếu ngân sách **dưới 500k cho 1-2 người**, Nồi cơm điện mini Comet 0.8L (250k) là lựa chọn hoàn hảo vừa túi tiền. Nếu gia đình **4-6 người**, bạn nên cân nhắc nâng ngân sách lên 690k để chọn Nồi cơm điện Sharp 1.8L bền bỉ nhiều năm!");
        }
        else if (lowerPrompt.Contains("giấy") || lowerPrompt.Contains("khăn"))
        {
            sb.AppendLine("• **Kinh nghiệm chọn giấy vệ sinh gia đình**: Nếu muốn **êm mềm dịu da không mủn**, Pulppy 10 cuộn 3 lớp (85k) là lựa chọn số 1 về chất lượng. Còn nếu cần **tiết kiệm chi phí sinh hoạt tối đa**, An An 10 cuộn không lõi (45k) sẽ giúp bạn tiết kiệm gần 50% chi phí!");
        }
        else if (lowerPrompt.Contains("dầu gội"))
        {
            sb.AppendLine("• Nếu ưu tiên **tiết kiệm mà vẫn gội mượt**, chọn Pantene 650ml (159k). Nhưng nếu muốn **sạch gàu mát lạnh sâu và dùng lâu 4-5 tháng**, Clear Men 630g (172k) là món hời nhất về p/p!");
        }
        else if (lowerPrompt.Contains("rửa chén") || lowerPrompt.Contains("tiết kiệm"))
        {
            sb.AppendLine("• **Mẹo tiết kiệm chi phí gia đình**: Mua Sunlight Can 3.6kg (105k) đơn giá chỉ ~29k/kg, tiết kiệm hơn 25% so với mua 5 chai 750g (145k). Hãy tận dụng chai cũ để chiết ra dùng dần nhé!");
        }
        else if (lowerPrompt.Contains("nồi") || lowerPrompt.Contains("chảo") || lowerPrompt.Contains("dùng lâu"))
        {
            sb.AppendLine("• Nếu dùng **bếp gas hoặc nhu cầu nấu thông thường**, chảo Sunhouse 24cm (165k) là quá đủ. Nhưng nếu dùng **bếp từ hoặc muốn bền 3+ năm không cong đáy**, hãy chọn chảo đáy từ Elmich 24cm (245k)!");
        }
        else if (lowerPrompt.Contains("em bé") || lowerPrompt.Contains("phân khúc nào"))
        {
            sb.AppendLine("• Với đồ dùng cho em bé, **tuyệt đối không chọn hàng giá rẻ trôi nổi thiếu kiểm định**. Khăn ướt Bobby (32k/80 miếng) thuộc Tier 2 chuẩn an toàn y tế không cồn, vừa lành tính cho da non của bé vừa rất hợp túi tiền.");
        }
        else
        {
            sb.AppendLine("• Bạn có thể xem bảng so sánh các phân khúc bên dưới và bấm nút **Thêm vào giỏ** ở thẻ sản phẩm phù hợp nhất với nhu cầu nhé!");
        }

        if (recommendations.Any() && recommendations.All(r => !r.IsWithinBudget))
        {
            var minPrice = recommendations.Min(r => r.FinalPrice);
            sb.Insert(0, $"⚠️ **Thông báo ngân sách**: Hiện tại siêu thị chưa có sản phẩm nào thuộc nhóm này dưới mức ngân sách yêu cầu. Bạn có thể cân nhắc nâng ngân sách từ **{minPrice:N0} đ** để sở hữu sản phẩm chất lượng đạt chuẩn, hoặc tham khảo các lựa chọn sát ngân sách nhất dưới đây:\n\n");
        }

        return sb.ToString();
    }
}
