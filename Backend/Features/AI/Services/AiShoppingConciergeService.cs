using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Services;

public class AiShoppingConciergeService : IAiShoppingConciergeService
{
    private readonly AppDbContext _dbContext;
    private readonly ILlmClient _llmClient;

    public AiShoppingConciergeService(AppDbContext dbContext, ILlmClient llmClient)
    {
        _dbContext = dbContext;
        _llmClient = llmClient;
    }

    private sealed record ProductBatchSummary(int TotalStock, DateTime MinExpiry);

    public async Task<MealPlanChatResponse> PlanMealAndShopAsync(MealPlanChatRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = request.UserPrompt?.Trim() ?? string.Empty;

        // Trích xuất số người ăn và ngân sách từ yêu cầu hoặc lịch sử chat
        int servings = request.NumberOfPeople ?? ParseServings(prompt, request.ConversationHistory);
        decimal targetBudget = request.TargetBudget ?? ParseBudget(prompt, request.ConversationHistory);
        if (targetBudget <= 0) targetBudget = 350000m;

        // Nhận diện ý định: Chào hỏi, Hỏi mẹo nấu ăn thuần túy, hay Lên mâm cơm/mua sắm
        if (IsGreeting(prompt))
        {
            return new MealPlanChatResponse
            {
                AssistantMessage = "Chào bạn! Em là Đầu Bếp & Trợ Lý Đi Chợ Thông Minh của Smart Supermarket 🛒. " +
                                   "Em có thể tư vấn các món ngon chuẩn vị Việt, mẹo nấu nướng từ chuyên gia, cân đối ngân sách và chuẩn bị sẵn mâm nguyên liệu từ kho siêu thị. " +
                                   "Hôm nay bạn muốn nấu món gì, hay cần em gợi ý mâm cơm ngon cho gia đình ạ?",
                HasMealSuggestion = false,
                DishName = string.Empty,
                Servings = servings,
                TotalEstimatedCost = 0,
                TotalSavedAmount = 0,
                IsWithinBudget = true,
                Ingredients = new List<MealIngredientItemDto>(),
                CookingTips = string.Empty
            };
        }

        if (IsCulinaryAdviceOnly(prompt))
        {
            string culinaryAdvice = GenerateCulinaryKnowledgeAdvice(prompt);
            return new MealPlanChatResponse
            {
                AssistantMessage = culinaryAdvice,
                HasMealSuggestion = false,
                DishName = string.Empty,
                Servings = servings,
                TotalEstimatedCost = 0,
                TotalSavedAmount = 0,
                IsWithinBudget = true,
                Ingredients = new List<MealIngredientItemDto>(),
                CookingTips = string.Empty
            };
        }

        // Xác định chủ đề món ăn phù hợp với yêu cầu
        string dishName = DetermineDishTheme(prompt, request.ConversationHistory);

        // Lấy danh sách sản phẩm thật từ kho hàng
        var dbProducts = await _dbContext.Products
            .Where(p => p.Status == ProductStatus.Active)
            .Include(p => p.Category)
            .ToListAsync(cancellationToken);

        // Lấy số tồn kho thực tế và hạn sử dụng từ lô hàng
        var batches = await _dbContext.InventoryBatches
            .Where(b => b.Quantity > 0)
            .ToListAsync(cancellationToken);

        var batchLookup = batches
            .GroupBy(b => b.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new ProductBatchSummary(g.Sum(x => x.Quantity), g.Min(x => x.ExpiryDate)));

        // Lọc nguyên liệu phù hợp với chủ đề món ăn dựa trên kho hàng thực tế
        var matchedItems = SelectIngredientsForTheme(dishName, dbProducts, batchLookup, servings, targetBudget, request.PrioritizePromotions);

        decimal totalCost = matchedItems.Sum(i => i.FinalPrice * i.QuantityToBuy);
        decimal originalTotal = matchedItems.Sum(i => i.RegularPrice * i.QuantityToBuy);
        decimal totalSaved = Math.Max(0, originalTotal - totalCost);

        // Chuẩn bị thông điệp đầu bếp và mẹo nấu ăn chính thống
        string message = GenerateChefMealMessage(dishName, servings, totalCost, targetBudget, totalSaved, matchedItems);
        string cookingTips = GenerateCookingTips(dishName);

        return new MealPlanChatResponse
        {
            AssistantMessage = message,
            DishName = dishName,
            Servings = servings,
            TotalEstimatedCost = totalCost,
            TotalSavedAmount = totalSaved,
            IsWithinBudget = totalCost <= targetBudget * 1.15m,
            HasMealSuggestion = true,
            Ingredients = matchedItems,
            CookingTips = cookingTips
        };
    }

    private static bool IsGreeting(string prompt)
    {
        string p = prompt.Trim().ToLowerInvariant();
        string[] greetings = { "xin chào", "chào bạn", "chào em", "chào shop", "chào ad", "hello", "hi", "bạn là ai", "bạn tên gì", "giúp gì được", "bạn làm được gì" };
        return greetings.Any(g => p == g || (p.StartsWith(g) && p.Length <= g.Length + 4));
    }

    private static bool IsCulinaryAdviceOnly(string prompt)
    {
        string p = prompt.ToLowerInvariant();
        string[] adviceTriggers = { "cách luộc", "bí quyết ướp", "ướp thế nào", "sao cho xanh", "không bị dai", "không bị tanh", "khử mùi", "tại sao nấu", "cách chọn thịt", "bảo quản ra sao" };
        string[] mealTriggers = { "lên thực đơn", "lên mâm", "người ăn", "ngân sách", "lẩu", "tối nay ăn gì", "hôm nay ăn gì", "trưa nay ăn gì", "mua nguyên liệu", "cho vào giỏ" };

        bool hasAdvice = adviceTriggers.Any(t => p.Contains(t));
        bool hasMeal = mealTriggers.Any(t => p.Contains(t));
        return hasAdvice && !hasMeal;
    }

    private static string GenerateCulinaryKnowledgeAdvice(string prompt)
    {
        string p = prompt.ToLowerInvariant();

        if (p.Contains("rau muống") || p.Contains("luộc rau"))
        {
            return "🥬 **Bí quyết luộc rau muống xanh mướt, giòn ngọt chuẩn đầu bếp:**\n\n" +
                   "1. **Lửa lớn, nước ngập:** Nấu nước thật sôi bùng trên lửa lớn, lượng nước phải ngập hoàn toàn rau.\n" +
                   "2. **Thêm muối hạt:** Cho 1/2 thìa cà phê muối vào nước sôi để giữ sắc tố diệp lục tươi tắn.\n" +
                   "3. **Không đậy nắp vung:** Thả rau vào dùng đũa dìm ngập nước ngay, giữ lửa lớn và mở nắp để rau thoát nhiệt tốt.\n" +
                   "4. **Sốc nhiệt nước đá:** Luộc nhanh 2 - 3 phút, vớt ra thả ngay vào âu nước đá lạnh 2 phút rồi vớt ráo. Rau sẽ giòn sần sật suốt nhiều tiếng.\n\n" +
                   "💡 Nước luộc rau chỉ cần vắt thêm chút chanh tươi hoặc dầm sấu là có ngay bát canh thanh mát giải nhiệt!";
        }

        if (p.Contains("thịt bò") || p.Contains("không bị dai") || p.Contains("ướp bò"))
        {
            return "🥩 **Bí quyết ướp & xào thịt bò mềm mọng, không bị dai:**\n\n" +
                   "1. **Cắt ngang thớ:** Nhìn rõ đường thớ thịt và dùng dao sắc thái lát mỏng ngang thớ. Thái dọc thớ thịt sẽ bị dai cứng.\n" +
                   "2. **Khóa ẩm bằng dầu & bột năng:** Ướp thịt bò với 1 thìa cà phê dầu ăn, 1/2 thìa bột bắp (hoặc bột năng), tỏi băm, tiêu và hạt nêm. Tránh ướp nhiều muối hay nước mắm sớm vì muối sẽ hút nước khiến thịt bị khô.\n" +
                   "3. **Xào lửa cực đại:** Đun chảo thật nóng, cho dầu vào phi tỏi thơm rồi trút thịt bò vào đảo nhanh tay 1-2 phút vừa chín tái thì tắt bếp ngay.";
        }

        if (p.Contains("mực") || p.Contains("cá") || p.Contains("không bị tanh") || p.Contains("khử mùi"))
        {
            return "🐟 **Bí quyết khử tanh hải sản & giữ trọn độ tươi giòn:**\n\n" +
                   "1. **Với cá tươi:** Rửa sạch màng đen trong bụng, xoa muối hạt và một chút rượu trắng hoặc nước cốt chanh lên thân cá trong 3 phút rồi rửa lại bằng nước sạch.\n" +
                   "2. **Với mực ống:** Bóc túi mực và mắt, khía vảy rồng mặt trong. Chần nhanh mực qua nước sôi có pha vài lát gừng đập dập trong 15 giây rồi vớt ra xả nước lạnh để mực giòn sần sật và không ra nước khi xào.";
        }

        if (p.Contains("sườn") || p.Contains("kho tàu"))
        {
            return "🍖 **Bí quyết ướp sườn & thịt kho tàu đậm đà, màu đẹp óng ánh:**\n\n" +
                   "1. **Chần sơ nước sôi:** Chần sườn hoặc thịt qua nước sôi có thả nhánh gừng đập dập và củ hành tím để sạch bọt cặn và thơm thịt.\n" +
                   "2. **Thắng đường caramen chuẩn:** Cho 1 thìa dầu ăn và 1.5 thìa đường vào chảo, đun lửa nhỏ đến khi đường sôi bọt khí li ti và chuyển sang màu cánh gián thì cho hành tỏi vào phi thơm rồi đổ thịt vào đảo đều.\n" +
                   "3. **Kho với nước dừa tươi:** Nước dừa giúp thịt mềm rục, mỡ trong veo béo ngậy mà không hề ngấy.";
        }

        return "👨‍🍳 **Mẹo ẩm thực từ chuyên gia:**\n" +
               "Khi chế biến món ăn Việt, sự cân bằng ngũ vị (chua - cay - mặn - ngọt - đắng) và độ tươi ngon của nguyên liệu là yếu tố then chốt. " +
               "Tại Smart Supermarket, toàn bộ thịt tươi, hải sản và rau củ đều được kiểm định nguồn gốc mỗi sáng. " +
               "Nếu bạn cần lên thực đơn chi tiết cho bữa cơm gia đình hôm nay, hãy nhắn số người ăn hoặc món bạn muốn nấu nhé!";
    }

    private static int ParseServings(string prompt, List<ChatMessageDto>? history)
    {
        var match = Regex.Match(prompt, @"(\d+)\s*(người|suất|phần|khách|thành viên)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int count) && count > 0 && count <= 20)
        {
            return count;
        }

        if (history != null && history.Any())
        {
            foreach (var msg in history.AsEnumerable().Reverse())
            {
                var hMatch = Regex.Match(msg.Content, @"(\d+)\s*(người|suất|phần|khách)", RegexOptions.IgnoreCase);
                if (hMatch.Success && int.TryParse(hMatch.Groups[1].Value, out int hCount) && hCount > 0 && hCount <= 20)
                {
                    return hCount;
                }
            }
        }

        return 4;
    }

    private static decimal ParseBudget(string prompt, List<ChatMessageDto>? history)
    {
        var textToSearch = prompt;
        var budget = ExtractBudgetFromString(textToSearch);
        if (budget.HasValue) return budget.Value;

        if (history != null && history.Any())
        {
            foreach (var msg in history.AsEnumerable().Reverse())
            {
                var hBudget = ExtractBudgetFromString(msg.Content);
                if (hBudget.HasValue) return hBudget.Value;
            }
        }

        return 350000m;
    }

    private static decimal? ExtractBudgetFromString(string text)
    {
        var matchK = Regex.Match(text, @"(\d+)\s*[kK]\b");
        if (matchK.Success && decimal.TryParse(matchK.Groups[1].Value, out decimal kVal))
        {
            return kVal * 1000m;
        }

        var matchNumber = Regex.Match(text, @"(\d{2,4})\s*(nghìn|ngàn)", RegexOptions.IgnoreCase);
        if (matchNumber.Success && decimal.TryParse(matchNumber.Groups[1].Value, out decimal nVal))
        {
            return nVal * 1000m;
        }

        var matchTrieu = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*(triệu|tr)", RegexOptions.IgnoreCase);
        if (matchTrieu.Success && decimal.TryParse(matchTrieu.Groups[1].Value.Replace(',', '.'), out decimal trVal))
        {
            return trVal * 1000000m;
        }

        var matchVnd = Regex.Match(text, @"(\d{4,9})\s*(?:đ|vnd|đồng)?\b", RegexOptions.IgnoreCase);
        if (matchVnd.Success && decimal.TryParse(matchVnd.Groups[1].Value, out decimal vndVal) && vndVal >= 20000)
        {
            return vndVal;
        }

        return null;
    }

    private static string DetermineDishTheme(string prompt, List<ChatMessageDto>? history)
    {
        string p = prompt.ToLowerInvariant();
        if (p.Contains("lẩu") || p.Contains("lau"))
        {
            if (p.Contains("hải sản") || p.Contains("tôm") || p.Contains("mực")) return "Lẩu Thái Hải Sản Chua Cay";
            if (p.Contains("bò")) return "Lẩu Bò Nấm Thập Cẩm";
            return "Lẩu Thái Chua Cay Thập Cẩm";
        }
        if (p.Contains("thịt kho") || p.Contains("kho tàu") || p.Contains("ba chỉ kho")) return "Thịt Ba Chỉ Kho Tàu & Canh Rau Muống";
        if (p.Contains("bò sốt vang") || p.Contains("sốt vang")) return "Bò Úc Sốt Vang & Bánh Mì";
        if (p.Contains("canh chua") || p.Contains("cá hồi")) return "Canh Chua Cá Hồi & Thịt Ba Chỉ Rang";
        if (p.Contains("chay") || p.Contains("thanh đạm") || p.Contains("nấm đậu")) return "Mâm Cơm Chay Nấm Đậu Thanh Đạm";
        if (p.Contains("sườn") || p.Contains("chua ngọt")) return "Sườn Non Xào Chua Ngọt & Canh Cải";
        if (p.Contains("gà") || p.Contains("kho gừng")) return "Gà Ta Kho Gừng Sả & Canh Rau";
        if (p.Contains("mực") || p.Contains("cần tỏi")) return "Mực Tươi Xào Cần Tỏi & Canh Cà Chua";

        // Tra cứu tiếp trong lịch sử hội thoại gần nhất nếu prompt hiện tại ngắn
        if (history != null && history.Any())
        {
            foreach (var msg in history.AsEnumerable().Reverse())
            {
                string hp = msg.Content.ToLowerInvariant();
                if (hp.Contains("lẩu")) return "Lẩu Thái Chua Cay Thập Cẩm";
                if (hp.Contains("bò sốt vang")) return "Bò Úc Sốt Vang & Bánh Mì";
                if (hp.Contains("thịt kho")) return "Thịt Ba Chỉ Kho Tàu & Canh Rau Muống";
                if (hp.Contains("sườn")) return "Sườn Non Xào Chua Ngọt & Canh Cải";
            }
        }

        return "Mâm Cơm Gia Đình Dinh Dưỡng";
    }

    private static List<MealIngredientItemDto> SelectIngredientsForTheme(
        string theme,
        List<Product> products,
        Dictionary<int, ProductBatchSummary> batchLookup,
        int servings,
        decimal targetBudget,
        bool prioritizePromotions)
    {
        var result = new List<MealIngredientItemDto>();
        var selectedProductIds = new HashSet<int>();

        // Danh sách từ khóa nguyên liệu chính xác theo ẩm thực truyền thống
        var keywords = new List<(string Keyword, int DefaultQty)>();

        if (theme.Contains("Lẩu Thái") || theme.Contains("Lẩu"))
        {
            keywords.Add(("bò", servings >= 4 ? 2 : 1));
            keywords.Add(("tôm", 1));
            keywords.Add(("mực", 1));
            keywords.Add(("nấm kim châm", 2));
            keywords.Add(("cải thảo", 1));
            keywords.Add(("rau muống", 1));
            keywords.Add(("ngô", 2));
            keywords.Add(("hạt nêm", 1));
        }
        else if (theme.Contains("Kho Tàu"))
        {
            keywords.Add(("ba chỉ", servings >= 4 ? 2 : 1));
            keywords.Add(("trứng", 1));
            keywords.Add(("nước mắm", 1));
            keywords.Add(("rau muống", 1));
            keywords.Add(("đường", 1));
            keywords.Add(("tiêu", 1));
        }
        else if (theme.Contains("Sốt Vang"))
        {
            keywords.Add(("bò", servings >= 4 ? 2 : 1));
            keywords.Add(("khoai tây", 1));
            keywords.Add(("cà rốt", 1));
            keywords.Add(("cà chua", 1));
            keywords.Add(("bánh mì", servings >= 4 ? 4 : 2));
            keywords.Add(("hạt nêm", 1));
        }
        else if (theme.Contains("Canh Chua") || theme.Contains("Cá Hồi"))
        {
            keywords.Add(("cá hồi", 1));
            keywords.Add(("thịt nạc", 1));
            keywords.Add(("cà chua", 1));
            keywords.Add(("rau muống", 1));
            keywords.Add(("nước mắm", 1));
        }
        else if (theme.Contains("Sườn Non") || theme.Contains("Chua Ngọt"))
        {
            keywords.Add(("sườn", servings >= 4 ? 2 : 1));
            keywords.Add(("cà chua", 1));
            keywords.Add(("đường", 1));
            keywords.Add(("nước mắm", 1));
            keywords.Add(("rau muống", 1));
        }
        else if (theme.Contains("Gà Ta") || theme.Contains("Gà Kho"))
        {
            keywords.Add(("gà", servings >= 4 ? 2 : 1));
            keywords.Add(("nước mắm", 1));
            keywords.Add(("hạt nêm", 1));
            keywords.Add(("cải thảo", 1));
            keywords.Add(("cà rốt", 1));
        }
        else if (theme.Contains("Mực Tươi") || theme.Contains("Mực"))
        {
            keywords.Add(("mực", 1));
            keywords.Add(("thịt ba chỉ", 1));
            keywords.Add(("cà chua", 1));
            keywords.Add(("rau muống", 1));
            keywords.Add(("nước mắm", 1));
        }
        else if (theme.Contains("Chay"))
        {
            keywords.Add(("nấm đùi gà", 2));
            keywords.Add(("nấm kim châm", 2));
            keywords.Add(("cải thảo", 1));
            keywords.Add(("rau muống", 1));
            keywords.Add(("cà rốt", 1));
            keywords.Add(("đường", 1));
        }
        else
        {
            keywords.Add(("thịt", 1));
            keywords.Add(("trứng", 1));
            keywords.Add(("rau", 1));
            keywords.Add(("cà chua", 1));
            keywords.Add(("nước mắm", 1));
        }

        foreach (var (kw, qty) in keywords)
        {
            var match = products
                .Where(p => !selectedProductIds.Contains(p.ProductId))
                .Where(p => p.ProductName.Contains(kw, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Price > 0)
                .FirstOrDefault();

            if (match == null) continue;

            selectedProductIds.Add(match.ProductId);

            // Kiểm tra tồn kho thực tế và áp dụng giảm giá cận date nếu có
            int inStock = 100;
            bool isNearExpiry = false;
            decimal discountPercent = 0;
            string promoNote = string.Empty;

            if (batchLookup.TryGetValue(match.ProductId, out var batchInfo))
            {
                inStock = batchInfo.TotalStock;
                DateTime minExp = batchInfo.MinExpiry;
                int daysLeft = (minExp - DateTime.UtcNow).Days;
                if (daysLeft <= 7 && prioritizePromotions)
                {
                    isNearExpiry = true;
                    discountPercent = 30m;
                    promoNote = "Flash Sale cận date giảm 30%";
                }
            }

            decimal regularPrice = match.Price;
            decimal finalPrice = discountPercent > 0
                ? Math.Round(regularPrice * (100m - discountPercent) / 100m, 0)
                : regularPrice;

            result.Add(new MealIngredientItemDto
            {
                ProductId = match.ProductId,
                ProductName = match.ProductName,
                Barcode = match.Barcode,
                ImageUrl = match.ImageUrl ?? "/images/products/default.png",
                Unit = match.Unit,
                QuantityToBuy = qty,
                RegularPrice = regularPrice,
                FinalPrice = finalPrice,
                DiscountPercent = discountPercent,
                IsPromotionOrNearExpiry = isNearExpiry,
                PromotionNote = promoNote,
                InStockQuantity = inStock
            });
        }

        // Tối ưu ngân sách nếu vượt quá giới hạn mong muốn
        decimal currentTotal = result.Sum(r => r.FinalPrice * r.QuantityToBuy);
        if (currentTotal > targetBudget * 1.15m && result.Count > 3)
        {
            var highestItem = result.OrderByDescending(r => r.FinalPrice * r.QuantityToBuy).FirstOrDefault();
            if (highestItem != null && highestItem.QuantityToBuy > 1)
            {
                highestItem.QuantityToBuy = 1;
            }
        }

        return result;
    }

    private static string GenerateChefMealMessage(
        string dishName,
        int servings,
        decimal totalCost,
        decimal targetBudget,
        decimal totalSaved,
        List<MealIngredientItemDto> ingredients)
    {
        var promoItems = ingredients.Where(i => i.IsPromotionOrNearExpiry).ToList();
        string promoText = promoItems.Any()
            ? $"Đặc biệt, siêu thị đang có chương trình Flash Sale ưu đãi cho {string.Join(", ", promoItems.Take(2).Select(i => i.ProductName))}, giúp bạn tiết kiệm được {totalSaved:N0} đ!"
            : "Toàn bộ nguyên liệu đều được tuyển chọn tươi mới từ các nhà cung cấp uy tín trong sáng nay.";

        return $"Dạ, với yêu cầu của bạn, em gợi ý mâm cơm **\"{dishName}\"** rất hợp khẩu vị cho **{servings} người ăn**.\n\n" +
               $"Tổng chi phí mâm nguyên liệu dự kiến là **{totalCost:N0} đ** (ngân sách dự kiến: {targetBudget:N0} đ). {promoText}\n\n" +
               "Mời bạn bấm vào nút bên dưới để xem chi tiết mâm nguyên liệu, tùy ý giữ lại hoặc bỏ bớt món theo ý thích trước khi thêm vào giỏ hàng nhé!";
    }

    private static string GenerateCookingTips(string theme)
    {
        if (theme.Contains("Lẩu"))
        {
            return "1. Rửa sạch rau nấm và cắt khúc vừa ăn. Ngâm nấm kim châm vào nước muối loãng 5 phút rồi để ráo.\n" +
                   "2. Nấu 1.8L nước sôi, cho ngô ngọt cắt khoanh và gói gia vị lẩu vào đun sôi 5-7 phút để nước dùng ngọt thanh tự nhiên.\n" +
                   "3. Nhúng thịt bò Úc Pacow và hải sản vừa chín tới để giữ vị ngọt mềm, ăn kèm nấm và rau xanh khi nước lẩu sôi bùng.";
        }
        if (theme.Contains("Kho Tàu"))
        {
            return "1. Thịt ba chỉ rửa sạch, chần sơ nước sôi rồi thái vuông 3-4cm. Ướp nước mắm cốt, hành tỏi băm, tiêu trong 20 phút.\n" +
                   "2. Thắng đường caramen màu cánh gián, cho thịt vào đảo săn đều rồi thêm nước dừa xiêm kho nhỏ lửa liu riu 35 phút.\n" +
                   "3. Thả trứng luộc vào kho tiếp 15 phút đến khi thịt mềm rục, mỡ trong veo và nước sốt sánh mịn óng ánh.";
        }
        if (theme.Contains("Sốt Vang"))
        {
            return "1. Thịt bò Úc thái quân cờ, chần sơ rồi ướp hạt nêm, tiêu, tỏi băm và một chút rượu vang đỏ trong 25 phút.\n" +
                   "2. Xào thịt bò với bơ tỏi cho xém cạnh, cho cà chua xào nhuyễn vào đảo đều, châm nước hầm nhỏ lửa 40 phút.\n" +
                   "3. Thêm khoai tây và cà rốt cắt khối vào hầm tiếp 15 phút đến khi rau củ mềm, nước sốt sánh sệt thì ăn kèm bánh mì giòn.";
        }
        if (theme.Contains("Chua Ngọt"))
        {
            return "1. Sườn non chặt miếng vừa ăn, chần qua nước sôi pha gừng để khử mùi và sạch bọt, để ráo.\n" +
                   "2. Rán sườn vàng xém 2 mặt. Pha sốt chua ngọt tỉ lệ vàng: 2 thìa nước mắm, 2 thìa đường, 2 thìa giấm, 1 thìa tương cà.\n" +
                   "3. Phi thơm hành tỏi, trút sườn và sốt vào đun lửa nhỏ đến khi sốt sền sệt quyện đều quanh từng miếng sườn.";
        }
        if (theme.Contains("Chay"))
        {
            return "1. Nấm đùi gà và nấm kim châm ngâm nước muối loãng 5 phút, rửa sạch, cắt miếng vừa ăn.\n" +
                   "2. Phi thơm sả ớt băm nhuyễn với dầu đậu nành Simply, cho nấm vào đảo nhanh tay ở lửa lớn để nấm giữ độ giòn ngọt.\n" +
                   "3. Nêm hạt nêm chay nấm hương và chút tiêu xay, dùng nóng cùng cơm trắng và canh rau muống luộc thanh mát.";
        }

        return "1. Sơ chế sạch sẽ các nguyên liệu tươi sống và để ráo nước hoàn toàn trước khi nấu.\n" +
               "2. Cân bằng gia vị: Ưu tiên nêm nếm theo nguyên tắc vừa miệng, đun lửa vừa để giữ trọn vitamin.\n" +
               "3. Trình bày món ăn đẹp mắt và thưởng thức nóng sốt cùng gia đình để món ăn đạt hương vị trọn vẹn nhất.";
    }
}
