using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.Services.Knowledge.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Services.Knowledge.Implementations;

public class KnowledgeRetrievalService : IKnowledgeRetrievalService
{
    private readonly IKnowledgeCache? _knowledgeCache;
    private readonly List<KnowledgeChunkDto> _knowledgeBase;

    public KnowledgeRetrievalService(IKnowledgeCache? knowledgeCache = null)
    {
        _knowledgeCache = knowledgeCache;
        _knowledgeBase = InitializeKnowledgeBase();
    }

    public async Task<KnowledgeQueryResultDto> RetrieveAsync(string query, int maxResults = 3, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new KnowledgeQueryResultDto { Query = query ?? string.Empty };
        }

        var normalized = query.Trim().ToLowerInvariant();
        var cacheKey = $"rag:query:{normalized.GetHashCode()}";

        if (_knowledgeCache != null)
        {
            var cached = await _knowledgeCache.GetKnowledgeAsync<KnowledgeQueryResultDto>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return cached;
            }
        }

        var queryTerms = ExtractTerms(normalized);
        var scored = new List<(KnowledgeChunkDto Chunk, double Score)>();

        foreach (var chunk in _knowledgeBase)
        {
            double score = CalculateRelevance(chunk, normalized, queryTerms);
            if (score > 0.15)
            {
                var copy = new KnowledgeChunkDto
                {
                    Id = chunk.Id,
                    Title = chunk.Title,
                    Type = chunk.Type,
                    Category = chunk.Category,
                    Content = chunk.Content,
                    Keywords = chunk.Keywords,
                    SourceDoc = chunk.SourceDoc,
                    RelevanceScore = Math.Round(score, 3)
                };
                scored.Add((copy, score));
            }
        }

        var results = scored.OrderByDescending(s => s.Score)
                            .Take(maxResults)
                            .Select(s => s.Chunk)
                            .ToList();

        var resultDto = new KnowledgeQueryResultDto
        {
            Query = query,
            Chunks = results
        };

        if (_knowledgeCache != null)
        {
            await _knowledgeCache.SetKnowledgeAsync(cacheKey, resultDto, TimeSpan.FromMinutes(30), cancellationToken);
        }
        return resultDto;
    }

    public Task<KnowledgeChunkDto?> GetExactMatchFaqAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question)) return Task.FromResult<KnowledgeChunkDto?>(null);

        var q = question.Trim().ToLowerInvariant();

        if (q.Contains("flash sale") && (q.Contains("an toàn") || q.Contains("cận date")))
        {
            return Task.FromResult(_knowledgeBase.FirstOrDefault(k => k.Id == "faq-flash-sale-safety"));
        }

        if (q.Contains("đổi trả") || (q.Contains("đổi") && q.Contains("trả")))
        {
            return Task.FromResult(_knowledgeBase.FirstOrDefault(k => k.Id == "faq-return-policy"));
        }

        if (q.Contains("voucher") && (q.Contains("thứ tự") || q.Contains("áp mã") || q.Contains("gộp")))
        {
            return Task.FromResult(_knowledgeBase.FirstOrDefault(k => k.Id == "policy-voucher-stacking"));
        }

        var match = _knowledgeBase.FirstOrDefault(k =>
            k.Type == KnowledgeType.FaqPolicy &&
            (k.Title.ToLowerInvariant().Contains(q) || q.Contains(k.Title.ToLowerInvariant())));

        return Task.FromResult(match);
    }

    public async Task<string> GroundPromptWithKnowledgeAsync(string query, CancellationToken cancellationToken = default)
    {
        var result = await RetrieveAsync(query, 2, cancellationToken);
        if (!result.HasRelevantKnowledge)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\n[OFFICIAL SUPERMARKET KNOWLEDGE & POLICIES]");
        foreach (var chunk in result.Chunks)
        {
            sb.AppendLine($"• [{chunk.Title}] (Nguồn: {chunk.SourceDoc}): {chunk.Content}");
        }
        sb.AppendLine("[HƯỚNG DẪN AI: Chỉ sử dụng các dữ kiện chính thống trên để trả lời, tuyệt đối không bịa đặt.]\n");

        return sb.ToString();
    }

    private static List<string> ExtractTerms(string text)
    {
        var words = Regex.Split(text, @"[\s,.\?!;:\(\)]+")
                         .Where(w => w.Length >= 2)
                         .Select(w => w.ToLowerInvariant())
                         .Distinct()
                         .ToList();
        return words;
    }

    private static double CalculateRelevance(KnowledgeChunkDto chunk, string fullQuery, List<string> queryTerms)
    {
        double score = 0.0;
        var lowerTitle = chunk.Title.ToLowerInvariant();
        var lowerContent = chunk.Content.ToLowerInvariant();

        // Exact substring match in title (high priority)
        if (fullQuery.Contains(lowerTitle) || lowerTitle.Contains(fullQuery))
        {
            score += 0.6;
        }

        // Keywords matches
        int matchedKeywords = 0;
        foreach (var kw in chunk.Keywords)
        {
            if (fullQuery.Contains(kw.ToLowerInvariant()))
            {
                matchedKeywords++;
            }
        }
        if (chunk.Keywords.Count > 0)
        {
            score += 0.4 * ((double)matchedKeywords / Math.Min(3, chunk.Keywords.Count));
        }

        // Word overlap
        int contentHits = queryTerms.Count(t => lowerContent.Contains(t));
        score += 0.2 * ((double)contentHits / Math.Max(1, queryTerms.Count));

        return Math.Min(1.0, score);
    }

    private static List<KnowledgeChunkDto> InitializeKnowledgeBase()
    {
        return new List<KnowledgeChunkDto>
        {
            // FAQ 1: Flash sale / cận date
            new()
            {
                Id = "faq-flash-sale-safety",
                Title = "Chính sách an toàn hàng Flash Sale cận date",
                Type = KnowledgeType.FaqPolicy,
                Category = "Chính sách chất lượng",
                SourceDoc = "docs/10_AI/knowledge/business_rules/faq/customer_faq.md",
                Keywords = new List<string> { "cận date", "flash sale", "an toàn", "hạn dùng", "chất lượng" },
                Content = "Các sản phẩm Flash Sale đều được bảo quản theo quy chuẩn chuỗi cung ứng lạnh nghiêm ngặt (0°C - 4°C) và còn hạn dùng từ 1 đến 3 ngày, hoàn toàn tươi ngon và an toàn khi chế biến trong ngày."
            },

            // FAQ 2: Đổi trả thực phẩm tươi sống
            new()
            {
                Id = "faq-return-policy",
                Title = "Chính sách đổi trả hàng tươi sống trong 24 giờ",
                Type = KnowledgeType.FaqPolicy,
                Category = "Chính sách đổi trả",
                SourceDoc = "docs/10_AI/knowledge/business_rules/faq/customer_faq.md",
                Keywords = new List<string> { "đổi trả", "hư hỏng", "dập nát", "hoàn tiền", "chính sách đổi" },
                Content = "Siêu thị hỗ trợ 1 đổi 1 ngay lập tức trong vòng 24h nếu sản phẩm tươi sống bị hư hỏng, dập nát hoặc không đạt chất lượng cam kết khi nhận hàng."
            },

            // FAQ 3: Thứ tự áp dụng Voucher
            new()
            {
                Id = "policy-voucher-stacking",
                Title = "Thứ tự ưu tiên và quy tắc gộp Voucher",
                Type = KnowledgeType.FaqPolicy,
                Category = "Chính sách khuyến mãi",
                SourceDoc = "docs/10_AI/knowledge/business_rules/voucher/voucher_priority.md",
                Keywords = new List<string> { "voucher", "mã giảm giá", "áp mã", "gộp mã", "thứ tự voucher" },
                Content = "Mỗi đơn hàng được áp dụng: 1 voucher giảm giá sản phẩm/đơn hàng + 1 voucher miễn phí vận chuyển + tích/tiêu điểm thành viên. Giảm giá áp dụng sau khi trừ giá khuyến mãi của từng món."
            },

            // Category Profile 1: Thịt tươi sống & Thủy hải sản
            new()
            {
                Id = "profile-fresh-meat",
                Title = "Hồ sơ ngành hàng Thịt & Thủy hải sản tươi",
                Type = KnowledgeType.CategoryProfile,
                Category = "Thịt, Thủy hải sản & Trứng",
                SourceDoc = "docs/10_AI/knowledge/supermarket/category_structure.md",
                Keywords = new List<string> { "thịt tươi", "thịt bò", "thịt heo", "hải sản", "bảo quản thịt" },
                Content = "Ngành hàng mã 9: Thịt ba chỉ MeatDeli, Nạc heo C.P, Tôm thẻ, Cá tươi. Lưu trữ kho lạnh 0°C - 4°C, HSD từ 3 - 7 ngày. AI ưu tiên kiểm tra lô date trước khi lên mâm."
            },

            // Category Profile 2: Rau củ quả VietGAP
            new()
            {
                Id = "profile-vegetables",
                Title = "Hồ sơ ngành hàng Rau củ quả VietGAP",
                Type = KnowledgeType.CategoryProfile,
                Category = "Rau củ quả tươi",
                SourceDoc = "docs/10_AI/knowledge/supermarket/category_structure.md",
                Keywords = new List<string> { "rau củ", "rau muống", "cải ngọt", "vietgap", "bảo quản rau" },
                Content = "Ngành hàng mã 4: 100% đạt chứng nhận VietGAP, sơ chế sạch. Lưu kho mát, HSD 2 - 5 ngày. Khuyên luộc ngập nước sôi có muối để giữ diệp lục xanh mướt."
            },

            // Recipe Guide: Luộc rau muống giòn xanh
            new()
            {
                Id = "recipe-rau-muong",
                Title = "Bí quyết luộc rau muống giòn xanh thanh nhiệt",
                Type = KnowledgeType.RecipeGuide,
                Category = "Cẩm nang Cookpad",
                SourceDoc = "docs/10_AI/knowledge/recipes/vietnamese_recipes.md",
                Keywords = new List<string> { "luộc rau muống", "xanh mướt", "giòn", "canh rau muống" },
                Content = "Đun sôi nước với 1/2 thìa cà phê muối hạt, thả rau ngập nước ở lửa lớn trong 2-3 phút, vớt ra ngâm ngay vào âu nước đá để rau giòn sần sật và giữ màu xanh lục bảo. Nước luộc dầm chanh hoặc sấu làm canh mát."
            },

            // Recipe Guide: Thịt kho tàu nước dừa
            new()
            {
                Id = "recipe-thit-kho-tau",
                Title = "Công thức Thịt ba chỉ kho tàu chuẩn vị Nam Bộ",
                Type = KnowledgeType.RecipeGuide,
                Category = "Cẩm nang Cookpad",
                SourceDoc = "docs/10_AI/knowledge/recipes/vietnamese_recipes.md",
                Keywords = new List<string> { "thịt kho tàu", "kho trứng", "nước dừa", "thịt ba chỉ" },
                Content = "Thịt ba chỉ thái vuông 3-4cm, ướp hành tỏi băm, nước mắm ngon, hạt nêm trong 30 phút. Kho cùng nước dừa xiêm tươi trên lửa nhỏ liu riu 45 phút cho mỡ trong veo, thịt mềm rục không nát."
            },

            // Dietary & Nutrition: Tiêu chuẩn Eat Clean & Lượng Protein
            new()
            {
                Id = "nutrition-standards",
                Title = "Tiêu chuẩn định lượng dinh dưỡng bữa ăn gia đình",
                Type = KnowledgeType.DietaryNutrition,
                Category = "Dinh dưỡng & Sức khỏe",
                SourceDoc = "docs/10_AI/knowledge/nutrition/dietary_standards.md",
                Keywords = new List<string> { "dinh dưỡng", "protein", "calo", "eat clean", "khẩu phần" },
                Content = "Mỗi người trưởng thành cần trung bình 120g - 150g đạm tươi sống (thịt/cá/trứng) và 150g - 200g rau xanh mỗi bữa chính để đảm bảo cân bằng năng lượng và vi chất."
            }
        };
    }
}
