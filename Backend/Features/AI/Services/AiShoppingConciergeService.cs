using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                MealOptions = new List<MealOptionDto>(),
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
                MealOptions = new List<MealOptionDto>(),
                CookingTips = string.Empty
            };
        }

        // Xác định chủ đề món ăn phù hợp với yêu cầu
        string mainDishTheme = DetermineDishTheme(prompt, request.ConversationHistory);

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

        // Xây dựng 2 đến 3 Option thực đơn khác nhau để khách hàng lựa chọn
        var mealOptions = BuildMealOptions(mainDishTheme, dbProducts, batchLookup, servings, targetBudget, request.PrioritizePromotions);

        var primaryOption = mealOptions.FirstOrDefault() ?? new MealOptionDto
        {
            DishName = mainDishTheme,
            OptionName = "Thực đơn tiêu chuẩn",
            Ingredients = new List<MealIngredientItemDto>()
        };

        // Chuẩn bị thông điệp đầu bếp có liệt kê gạch đầu dòng rõ từng món và giá tiền
        string message = GenerateChefMealMessageWithMultiOptions(servings, targetBudget, mealOptions);

        return new MealPlanChatResponse
        {
            AssistantMessage = message,
            DishName = primaryOption.DishName,
            Servings = servings,
            TotalEstimatedCost = primaryOption.TotalEstimatedCost,
            TotalSavedAmount = primaryOption.TotalSavedAmount,
            IsWithinBudget = primaryOption.TotalEstimatedCost <= targetBudget * 1.15m,
            HasMealSuggestion = true,
            Ingredients = primaryOption.Ingredients,
            MealOptions = mealOptions,
            CookingTips = primaryOption.CookingTips
        };
    }

    private static List<MealOptionDto> BuildMealOptions(
        string theme,
        List<Product> products,
        Dictionary<int, ProductBatchSummary> batchLookup,
        int servings,
        decimal targetBudget,
        bool prioritizePromotions)
    {
        var options = new List<MealOptionDto>();

        if (theme.Contains("Lẩu"))
        {
            // Option 1: Lẩu Tiêu Chuẩn (Vừa vặn ngân sách, tươi ngon)
            var opt1Keywords = new List<(string Keyword, int DefaultQty)>
            {
                ("bò", servings >= 4 ? 1 : 1),
                ("tôm", 1),
                ("nấm kim châm", 2),
                ("cải thảo", 1),
                ("ngô", 2),
                ("hạt nêm", 1)
            };
            var items1 = MatchProductsForKeywords(opt1Keywords, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Lẩu Tiêu Chuẩn (Tiết kiệm)",
                DishName = "Lẩu Thái Chua Cay Tiêu Chuẩn",
                Description = "Đầy đủ thịt bò tươi, tôm thẻ, nấm kim châm & rau củ thanh ngọt.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Lẩu")
            });

            // Option 2: Lẩu Thịnh Soạn (Nhiều thịt bò Pacow, tôm mực, nấm đùi gà)
            var opt2Keywords = new List<(string Keyword, int DefaultQty)>
            {
                ("bò", servings >= 4 ? 2 : 1),
                ("tôm", 1),
                ("mực", 1),
                ("nấm kim châm", 2),
                ("nấm đùi gà", 1),
                ("cải thảo", 1),
                ("rau muống", 1),
                ("ngô", 2)
            };
            var items2 = MatchProductsForKeywords(opt2Keywords, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Lẩu Hải Sản & Bò Thịnh Soạn",
                DishName = "Lẩu Thái Hải Sản & Bò Úc Đặc Biệt",
                Description = "Gấp đôi thịt bò Úc Pacow, tôm tươi, mực ống giòn sần sật và nấm tươi phong phú.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Lẩu")
            });

            // Option 3: Mâm Lẩu Bò Nấm Dưỡng Sinh
            var opt3Keywords = new List<(string Keyword, int DefaultQty)>
            {
                ("bò", servings >= 4 ? 2 : 1),
                ("nấm kim châm", 2),
                ("nấm đùi gà", 2),
                ("cải thảo", 1),
                ("ngô", 2),
                ("hạt nêm", 1)
            };
            var items3 = MatchProductsForKeywords(opt3Keywords, products, batchLookup, prioritizePromotions);
            decimal cost3 = items3.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig3 = items3.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 3: Lẩu Bò & Nấm Thập Cẩm Thanh Ngọt",
                DishName = "Lẩu Bò Nấm Tươi Dưỡng Sinh",
                Description = "Vị nước dùng ngọt thanh từ ngô ngọt và các loại nấm tươi Hàn Quốc, nhúng bò Úc mềm mọng.",
                TotalEstimatedCost = cost3,
                TotalSavedAmount = Math.Max(0, orig3 - cost3),
                Ingredients = items3,
                CookingTips = GenerateCookingTips("Lẩu")
            });
        }
        else if (theme.Contains("Kho Tàu"))
        {
            // Option 1: Mâm Cơm Kho Tàu Tiêu Chuẩn
            var opt1Keywords = new List<(string Keyword, int DefaultQty)>
            {
                ("ba chỉ", servings >= 4 ? 2 : 1),
                ("trứng", 1),
                ("rau muống", 1),
                ("nước mắm", 1)
            };
            var items1 = MatchProductsForKeywords(opt1Keywords, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Thịt Kho Tàu & Canh Rau Muống",
                DishName = "Thịt Ba Chỉ Kho Tàu & Canh Rau Muống",
                Description = "Thịt ba chỉ MeatDeli kho trứng mềm rục, ăn kèm canh rau muống dầm chanh thanh mát.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Kho Tàu")
            });

            // Option 2: Mâm Cơm Gia Đình Đầy Đặn (Thêm Sườn Hoặc Đậu Phụ)
            var opt2Keywords = new List<(string Keyword, int DefaultQty)>
            {
                ("ba chỉ", servings >= 4 ? 2 : 1),
                ("trứng", 1),
                ("cà chua", 1),
                ("rau muống", 1),
                ("nước mắm", 1),
                ("đường", 1)
            };
            var items2 = MatchProductsForKeywords(opt2Keywords, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Thịt Kho Tàu & Canh Cà Chua Trứng",
                DishName = "Mâm Cơm Gia Đình Thịt Kho & Canh Cà Chua",
                Description = "Thịt ba chỉ kho béo ngậy kèm bát canh trứng cà chua thơm lừng.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Kho Tàu")
            });
        }
        else if (theme.Contains("Sốt Vang"))
        {
            var opt1Keywords = new List<(string Keyword, int DefaultQty)>
            {
                ("bò", servings >= 4 ? 2 : 1),
                ("khoai tây", 1),
                ("cà rốt", 1),
                ("cà chua", 1),
                ("bánh mì", servings >= 4 ? 4 : 2)
            };
            var items1 = MatchProductsForKeywords(opt1Keywords, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Bò Úc Sốt Vang Tiêu Chuẩn & Bánh Mì",
                DishName = "Bò Úc Sốt Vang Bánh Mì",
                Description = "Thịt bò Úc thái vuông om cà chua sánh mịn, khoai tây cà rốt mềm ngọt.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Sốt Vang")
            });
        }
        else
        {
            // Mâm cơm gia đình đa dạng
            var opt1Keywords = new List<(string Keyword, int DefaultQty)>
            {
                ("sườn", servings >= 4 ? 2 : 1),
                ("cà chua", 1),
                ("rau muống", 1),
                ("nước mắm", 1)
            };
            var items1 = MatchProductsForKeywords(opt1Keywords, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Sườn Xào Chua Ngọt & Canh Rau Muống",
                DishName = "Sườn Non Xào Chua Ngọt & Canh Rau Muống",
                Description = "Sườn non xào đậm đà óng ánh, canh rau thanh mát vừa miệng.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Chua Ngọt")
            });

            var opt2Keywords = new List<(string Keyword, int DefaultQty)>
            {
                ("ba chỉ", servings >= 4 ? 2 : 1),
                ("trứng", 1),
                ("rau muống", 1),
                ("nước mắm", 1)
            };
            var items2 = MatchProductsForKeywords(opt2Keywords, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Thịt Ba Chỉ Kho Tàu Đậm Vị",
                DishName = "Thịt Ba Chỉ Kho Tàu & Canh Rau",
                Description = "Thịt ba chỉ thái miếng vuông kho vàng óng thơm ngậy cùng trứng luộc.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Kho Tàu")
            });
        }

        return options;
    }

    private static List<MealIngredientItemDto> MatchProductsForKeywords(
        List<(string Keyword, int DefaultQty)> keywords,
        List<Product> products,
        Dictionary<int, ProductBatchSummary> batchLookup,
        bool prioritizePromotions)
    {
        var result = new List<MealIngredientItemDto>();
        var selectedProductIds = new HashSet<int>();

        foreach (var (kw, qty) in keywords)
        {
            var match = products
                .Where(p => !selectedProductIds.Contains(p.ProductId))
                .Where(p => p.ProductName.Contains(kw, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Price > 0)
                .FirstOrDefault();

            if (match == null) continue;

            selectedProductIds.Add(match.ProductId);

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

        return result;
    }

    private static string GenerateChefMealMessageWithMultiOptions(
        int servings,
        decimal targetBudget,
        List<MealOptionDto> options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Dạ, với yêu cầu của bạn, em đã lên **{options.Count} lựa chọn thực đơn** phù hợp cho **{servings} người ăn** (ngân sách dự kiến: {targetBudget:N0} đ):\n");

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            string icon = i == 0 ? "🍲" : (i == 1 ? "✨" : "🌿");
            sb.AppendLine($"{icon} **{opt.OptionName} (Tổng: {opt.TotalEstimatedCost:N0} đ)**");
            if (!string.IsNullOrEmpty(opt.Description))
            {
                sb.AppendLine($"  *Đặc điểm: {opt.Description}*");
            }
            sb.AppendLine("  **Danh sách nguyên liệu:**");

            foreach (var ing in opt.Ingredients)
            {
                decimal lineTotal = ing.FinalPrice * ing.QuantityToBuy;
                string promoTag = ing.DiscountPercent > 0 ? $" *(Giảm {ing.DiscountPercent}% Flash Sale)*" : "";
                sb.AppendLine($"  • **{ing.ProductName}** ({ing.QuantityToBuy} {ing.Unit}): **{lineTotal:N0} đ**{promoTag}");
            }

            if (opt.TotalSavedAmount > 0)
            {
                sb.AppendLine($"  💰 *Tiết kiệm hôm nay: -{opt.TotalSavedAmount:N0} đ*");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Mời bạn bấm vào nút lựa chọn bên dưới để xem chi tiết mâm nguyên liệu, tùy ý thêm/bớt món trước khi nạp vào giỏ hàng nhé!");
        return sb.ToString().TrimEnd();
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
