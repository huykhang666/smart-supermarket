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

    private sealed record IngredientQuery(
        int? CategoryId,
        string Keyword,
        int DefaultQty,
        string? ExcludeWord = null
    );

    public async Task<MealPlanChatResponse> PlanMealAndShopAsync(MealPlanChatRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = request.UserPrompt?.Trim() ?? string.Empty;

        int servings = request.NumberOfPeople ?? ParseServings(prompt, request.ConversationHistory);
        decimal targetBudget = request.TargetBudget ?? ParseBudget(prompt, request.ConversationHistory);
        if (targetBudget <= 0) targetBudget = 350000m;

        if (IsGreeting(prompt))
        {
            string greetingMsg = "Chào bạn! Mình là Đầu Bếp & Trợ Lý Đi Chợ Thông Minh của Smart Supermarket 🛒.\n\n" +
                                 "Mình có thể gợi ý các mâm cơm gia đình hàng ngày chuẩn vị (theo cẩm nang Cookpad), tư vấn bí quyết chế biến thơm ngon bất bại, cân đối ngân sách và chuẩn bị sẵn mâm nguyên liệu tươi mới từ kho siêu thị.\n\n" +
                                 "Hôm nay bạn muốn nấu món gì hay cần mình gợi ý mâm cơm ngon cho gia đình ạ?";

            try
            {
                string greetingPrompt = $"Khách hàng vừa chào: \"{prompt}\". Hãy đáp lại thân thiện bằng tiếng Việt, xưng là 'mình' và gọi 'bạn', giới thiệu bản thân là Trợ lý Đầu bếp Smart Supermarket và hỏi xem khách muốn nấu món gì hoặc cần mâm cơm cho mấy người ăn.";
                var geminiGreeting = await _llmClient.GenerateAsync(greetingPrompt, null, false, cancellationToken);
                if (!string.IsNullOrWhiteSpace(geminiGreeting) &&
                    !geminiGreeting.StartsWith("[Gemini API chưa") &&
                    !geminiGreeting.StartsWith("[Lỗi Gemini API") &&
                    !geminiGreeting.StartsWith("{\"error\":"))
                {
                    greetingMsg = geminiGreeting;
                }
            }
            catch
            {
            }

            return new MealPlanChatResponse
            {
                AssistantMessage = greetingMsg,
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

            try
            {
                string adviceSystem = "Bạn là Đầu Bếp chuyên gia am hiểu cẩm nang nấu ăn hàng ngày của Cookpad Việt Nam. Bạn xưng 'mình' và gọi 'bạn'. Tư vấn mẹo nấu ăn chính xác, súc tích, gạch đầu dòng rõ ràng, không lan man.";
                var llmAdvice = await _llmClient.GenerateAsync(prompt, adviceSystem, false, cancellationToken);
                if (!string.IsNullOrWhiteSpace(llmAdvice) &&
                    !llmAdvice.StartsWith("[Gemini API chưa") &&
                    !llmAdvice.StartsWith("[Lỗi Gemini API") &&
                    !llmAdvice.StartsWith("{\"error\":"))
                {
                    culinaryAdvice = llmAdvice;
                }
            }
            catch
            {
            }

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

        string mainDishTheme = DetermineDishTheme(prompt, targetBudget, servings, request.ConversationHistory);

        // Lấy danh sách sản phẩm từ kho hàng
        var dbProducts = await _dbContext.Products
            .Include(p => p.Category)
            .ToListAsync(cancellationToken);

        var batches = await _dbContext.InventoryBatches
            .Where(b => b.Quantity > 0)
            .ToListAsync(cancellationToken);

        var batchLookup = batches
            .GroupBy(b => b.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new ProductBatchSummary(g.Sum(x => x.Quantity), g.Min(x => x.ExpiryDate)));

        var mealOptions = BuildMealOptions(mainDishTheme, dbProducts, batchLookup, servings, targetBudget, request.PrioritizePromotions);

        var primaryOption = mealOptions.FirstOrDefault() ?? new MealOptionDto
        {
            DishName = mainDishTheme,
            OptionName = "Thực đơn tiêu chuẩn",
            Ingredients = new List<MealIngredientItemDto>()
        };

        string message = GenerateChefMealMessageWithMultiOptions(servings, targetBudget, mealOptions);

        string systemInstruction =
            "Bạn là Đầu Bếp & Trợ Lý Đi Chợ Thông Minh của Smart Supermarket. " +
            "Bạn xưng là 'mình' và gọi người dùng là 'bạn'. " +
            "Hãy tham khảo công thức nấu ăn hàng ngày chuẩn vị từ Cookpad Việt Nam. " +
            "Tư vấn thân thiện, súc tích, tóm tắt các lựa chọn thực đơn đã được thiết kế bên dưới, không liệt kê lại toàn bộ chi tiết bảng giá từng món vì hệ thống đã hiển thị bên dưới. " +
            "Căn lề sạch sẽ và nhiệt tình.";

        string aiPrompt = $"Khách hàng hỏi: \"{prompt}\". " +
            $"Số người ăn: {servings} người, ngân sách: {targetBudget:N0} đ. " +
            $"Các lựa chọn thực đơn đã được thiết kế từ kho hàng siêu thị:\n" +
            string.Join("\n", mealOptions.Select((o, i) => $"- Lựa chọn {i + 1}: {o.OptionName} (Tổng chi phí: {o.TotalEstimatedCost:N0} đ). Mô tả: {o.Description}. Món chính: {string.Join(", ", o.Ingredients.Take(4).Select(x => x.ProductName))}")) +
            "\nHãy viết lời tư vấn của đầu bếp gửi tới khách hàng, xưng là 'mình' và gọi 'bạn'.";

        try
        {
            var llmResponse = await _llmClient.GenerateAsync(aiPrompt, systemInstruction, false, cancellationToken);
            if (!string.IsNullOrWhiteSpace(llmResponse) &&
                !llmResponse.StartsWith("[Gemini API chưa") &&
                !llmResponse.StartsWith("[Lỗi Gemini API") &&
                !llmResponse.StartsWith("{\"error\":"))
            {
                message = llmResponse;
            }
        }
        catch
        {
        }

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

        if (theme == "Bữa Ăn Tiết Kiệm")
        {
            var q1 = new List<IngredientQuery>
            {
                new(9, "trứng gà", 1),
                new(4, "rau muống", 1)
            };
            var items1 = MatchProductsForQueries(q1, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Cơm Trứng Chiên & Canh Rau Muống Luộc",
                DishName = "Cơm Trứng Gà Chiên & Canh Rau Muống",
                Description = "Trứng gà Ba Huân chiên vàng xốp, ăn kèm canh rau muống xanh giòn dầm chanh thanh mát.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Trứng")
            });

            var q2 = new List<IngredientQuery>
            {
                new(10, "omachi", servings > 1 ? servings : 1),
                new(10, "xúc xích", 1),
                new(4, "cải ngọt", 1)
            };
            var items2 = MatchProductsForQueries(q2, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Mì Omachi Xúc Xích & Cải Ngọt Siêu Tốc",
                DishName = "Mì Nấu Xúc Xích Vissan & Cải Ngọt",
                Description = "Mì Omachi ngũ quả kèm xúc xích Vissan và rau cải ngọt VietGAP giòn ngọt, chế biến nhanh trong 5 phút.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Mì")
            });

            var q3 = new List<IngredientQuery>
            {
                new(10, "cá nục", 1),
                new(4, "dưa leo", 1)
            };
            var items3 = MatchProductsForQueries(q3, products, batchLookup, prioritizePromotions);
            decimal cost3 = items3.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig3 = items3.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 3: Cá Hộp Ba Cô Gái & Dưa Leo Baby",
                DishName = "Cá Nục Sốt Cà & Dưa Leo Baby",
                Description = "Cá nục sốt cà chua Ba Cô Gái sốt sánh mịn, ăn kèm dưa leo baby giòn mát và cơm nóng siêu đưa cơm.",
                TotalEstimatedCost = cost3,
                TotalSavedAmount = Math.Max(0, orig3 - cost3),
                Ingredients = items3,
                CookingTips = GenerateCookingTips("Cá Hộp")
            });
        }
        else if (theme == "Lẩu")
        {
            var q1 = new List<IngredientQuery>
            {
                new(9, "bò", servings >= 4 ? 2 : 1),
                new(9, "tôm", 1),
                new(4, "nấm kim châm", 2),
                new(4, "cải thảo", 1),
                new(4, "bắp ngọt", 2)
            };
            var items1 = MatchProductsForQueries(q1, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Lẩu Thái Chua Cay Tiêu Chuẩn",
                DishName = "Lẩu Thái Chua Cay Bò & Tôm",
                Description = "Bò Úc Pacow thái lát mềm mọng kết hợp tôm thẻ tươi sống và nấm kim châm giòn ngọt.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Lẩu")
            });

            var q2 = new List<IngredientQuery>
            {
                new(9, "bò", servings >= 4 ? 2 : 1),
                new(9, "tôm", 1),
                new(9, "mực", 1),
                new(4, "nấm kim châm", 2),
                new(4, "cải thảo", 1)
            };
            var items2 = MatchProductsForQueries(q2, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Lẩu Hải Sản & Bò Úc Thịnh Soạn",
                DishName = "Lẩu Thái Thập Cẩm Bò - Tôm - Mực",
                Description = "Đại tiệc lẩu với bộ ba bò Úc, tôm tươi và mực ống giòn sần sật.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Lẩu")
            });

            var q3 = new List<IngredientQuery>
            {
                new(9, "bò", servings >= 4 ? 2 : 1),
                new(4, "nấm kim châm", 2),
                new(4, "nấm đùi gà", 1),
                new(4, "bắp ngọt", 2),
                new(4, "cải thảo", 1)
            };
            var items3 = MatchProductsForQueries(q3, products, batchLookup, prioritizePromotions);
            decimal cost3 = items3.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig3 = items3.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 3: Lẩu Bò & Nấm Tươi Dưỡng Sinh",
                DishName = "Lẩu Bò Nấm Tươi Dưỡng Sinh",
                Description = "Vị nước dùng ngọt thanh từ ngô ngọt Mỹ và các loại nấm tươi sạch Hàn Quốc.",
                TotalEstimatedCost = cost3,
                TotalSavedAmount = Math.Max(0, orig3 - cost3),
                Ingredients = items3,
                CookingTips = GenerateCookingTips("Lẩu")
            });
        }
        else if (theme == "Thịt Kho Tàu")
        {
            var q1 = new List<IngredientQuery>
            {
                new(9, "ba chỉ", servings >= 4 ? 2 : 1),
                new(9, "trứng gà", 1),
                new(4, "rau muống", 1)
            };
            var items1 = MatchProductsForQueries(q1, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Thịt Ba Chỉ Kho Tàu & Canh Rau Muống",
                DishName = "Thịt Ba Chỉ Kho Tàu & Canh Rau Muống",
                Description = "Thịt ba chỉ MeatDeli kho trứng mềm rục vàng óng, ăn kèm canh rau muống thanh mát.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Kho Tàu")
            });

            var q2 = new List<IngredientQuery>
            {
                new(9, "nạc dăm", servings >= 4 ? 2 : 1),
                new(4, "cải ngọt", 1),
                new(4, "cà chua", 1)
            };
            var items2 = MatchProductsForQueries(q2, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Thịt Nạc Dăm Xào Cải Ngọt & Canh Cà Chua",
                DishName = "Thịt Nạc Dăm Xào Cải & Canh Cà Chua",
                Description = "Thịt nạc dăm mềm ngọt xào cải xanh giòn, kèm bát canh cà chua trứng dịu mát.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Kho Tàu")
            });

            var q3 = new List<IngredientQuery>
            {
                new(9, "ba rọi", servings >= 4 ? 2 : 1),
                new(4, "mướp hương", 1),
                new(4, "hành lá", 1)
            };
            var items3 = MatchProductsForQueries(q3, products, batchLookup, prioritizePromotions);
            decimal cost3 = items3.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig3 = items3.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 3: Thịt Ba Rọi Rang Cháy Cạnh & Canh Mướp",
                DishName = "Thịt Ba Rọi Rang Cháy Cạnh & Canh Mướp Hương",
                Description = "Thịt ba rọi rút sườn C.P rang xém cạnh thơm nức mùi hành hoa, canh mướp ngọt lành.",
                TotalEstimatedCost = cost3,
                TotalSavedAmount = Math.Max(0, orig3 - cost3),
                Ingredients = items3,
                CookingTips = GenerateCookingTips("Kho Tàu")
            });
        }
        else if (theme == "Sườn Non")
        {
            var q1 = new List<IngredientQuery>
            {
                new(9, "sườn non", servings >= 4 ? 2 : 1),
                new(4, "cà chua", 1),
                new(4, "rau muống", 1)
            };
            var items1 = MatchProductsForQueries(q1, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Sườn Non Xào Chua Ngọt & Canh Rau Muống",
                DishName = "Sườn Non Xào Chua Ngọt & Canh Rau Muống",
                Description = "Sườn non tươi C.P đảo sốt cà chua óng ả đậm đà, canh rau muống luộc giòn mát.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Chua Ngọt")
            });

            var q2 = new List<IngredientQuery>
            {
                new(9, "sườn non", servings >= 4 ? 2 : 1),
                new(4, "cải ngọt", 1),
                new(9, "trứng gà", 1)
            };
            var items2 = MatchProductsForQueries(q2, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Canh Sườn Cải Ngọt & Trứng Chiên",
                DishName = "Canh Sườn Non Nấu Cải Ngọt & Trứng Chiên",
                Description = "Nước canh sườn ngọt lịm từ xương, thêm đĩa trứng chiên xốp vàng ruộm.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Chua Ngọt")
            });

            var q3 = new List<IngredientQuery>
            {
                new(9, "sườn non", servings >= 4 ? 2 : 1),
                new(4, "mướp hương", 1),
                new(4, "hành lá", 1)
            };
            var items3 = MatchProductsForQueries(q3, products, batchLookup, prioritizePromotions);
            decimal cost3 = items3.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig3 = items3.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 3: Sườn Heo Rim Mặn Ngọt & Canh Mướp",
                DishName = "Sườn Non Rim Mặn Ngọt & Canh Mướp Hương",
                Description = "Sườn chặt nhỏ rim keo thơm lừng hành tỏi, canh mướp đồng quê thơm ngát.",
                TotalEstimatedCost = cost3,
                TotalSavedAmount = Math.Max(0, orig3 - cost3),
                Ingredients = items3,
                CookingTips = GenerateCookingTips("Chua Ngọt")
            });
        }
        else if (theme == "Thịt Gà")
        {
            var q1 = new List<IngredientQuery>
            {
                new(9, "đùi gà", servings >= 4 ? 2 : 1),
                new(4, "bắp cải trắng", 1),
                new(4, "hành lá", 1)
            };
            var items1 = MatchProductsForQueries(q1, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Đùi Gà Rim Mắm Tỏi & Canh Bắp Cải",
                DishName = "Đùi Gà Rim Mắm Tỏi & Canh Bắp Cải",
                Description = "Đùi gà C.P thịt chắc rim đậm đà nước mắm tỏi ớt, canh bắp cải luộc chấm trứng hoặc dầm sấu.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Gà")
            });

            var q2 = new List<IngredientQuery>
            {
                new(9, "ức gà", servings >= 4 ? 2 : 1),
                new(4, "nấm đùi gà", 1),
                new(4, "cải ngọt", 1)
            };
            var items2 = MatchProductsForQueries(q2, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Ức Gà Xào Nấm Đùi Gà & Canh Cải",
                DishName = "Ức Gà Xào Nấm Đùi Gà & Canh Cải Ngọt",
                Description = "Ức gà mềm giàu đạm xào nấm đùi gà giòn sần sật, thanh nhẹ tốt cho sức khỏe.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Gà")
            });

            var q3 = new List<IngredientQuery>
            {
                new(9, "cánh gà", servings >= 4 ? 2 : 1),
                new(4, "cà chua", 1),
                new(4, "hành lá", 1)
            };
            var items3 = MatchProductsForQueries(q3, products, batchLookup, prioritizePromotions);
            decimal cost3 = items3.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig3 = items3.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 3: Cánh Gà Chiên & Canh Cà Chua Trứng",
                DishName = "Cánh Gà Chiên Nước Mắm & Canh Cà Chua",
                Description = "Cánh gà chiên da giòn rụm óng ánh, ăn kèm canh cà chua thanh dịu ngọt ngào.",
                TotalEstimatedCost = cost3,
                TotalSavedAmount = Math.Max(0, orig3 - cost3),
                Ingredients = items3,
                CookingTips = GenerateCookingTips("Gà")
            });
        }
        else if (theme == "Bò Úc")
        {
            var q1 = new List<IngredientQuery>
            {
                new(9, "bò", servings >= 4 ? 2 : 1),
                new(4, "khoai tây", 1),
                new(4, "cà rốt", 1),
                new(4, "cà chua", 1)
            };
            var items1 = MatchProductsForQueries(q1, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Bò Úc Sốt Vang Tiêu Chuẩn",
                DishName = "Bò Úc Sốt Vang Khoai Tây Cà Rốt",
                Description = "Thịt bò Úc Pacow thái vuông om mềm sánh sệt cùng khoai tây vàng và cà rốt Đà Lạt.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Sốt Vang")
            });

            var q2 = new List<IngredientQuery>
            {
                new(9, "bò", servings >= 4 ? 2 : 1),
                new(4, "nấm kim châm", 1),
                new(4, "cải ngọt", 1)
            };
            var items2 = MatchProductsForQueries(q2, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Thịt Bò Xào Nấm Kim Châm & Cải Ngọt",
                DishName = "Bò Úc Xào Nấm Kim Châm & Cải Xanh",
                Description = "Thịt bò xào lửa lớn mềm mọng, nấm kim châm giòn sần sật cuốn hút.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Sốt Vang")
            });

            var q3 = new List<IngredientQuery>
            {
                new(9, "bò", servings >= 4 ? 2 : 1),
                new(4, "cà chua", 1),
                new(9, "trứng gà", 1)
            };
            var items3 = MatchProductsForQueries(q3, products, batchLookup, prioritizePromotions);
            decimal cost3 = items3.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig3 = items3.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 3: Canh Thịt Bò Cà Chua Trứng Thanh Mát",
                DishName = "Canh Thịt Bò Cà Chua Trứng",
                Description = "Nước canh thanh ngọt bổ dưỡng, màu sắc rực rỡ bắt mắt và nhẹ bụng.",
                TotalEstimatedCost = cost3,
                TotalSavedAmount = Math.Max(0, orig3 - cost3),
                Ingredients = items3,
                CookingTips = GenerateCookingTips("Sốt Vang")
            });
        }
        else if (theme == "Món Chay")
        {
            var q1 = new List<IngredientQuery>
            {
                new(4, "nấm đùi gà", 1),
                new(4, "nấm kim châm", 1),
                new(4, "cải thảo", 1),
                new(4, "mướp hương", 1)
            };
            var items1 = MatchProductsForQueries(q1, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Mâm Nấm Xào & Canh Cải Thảo Thanh Đạm",
                DishName = "Mâm Cơm Chay Nấm Tươi & Cải Thảo",
                Description = "Nấm đùi gà xào sả ớt giòn thơm, ăn cùng canh cải thảo ngọt thanh mát lành.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Chay")
            });

            var q2 = new List<IngredientQuery>
            {
                new(4, "nấm đùi gà", 1),
                new(4, "bắp ngọt", 2),
                new(4, "rau muống", 1),
                new(4, "bắp cải trắng", 1)
            };
            var items2 = MatchProductsForQueries(q2, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Canh Ngô Ngọt Nấm & Rau Muống Xào",
                DishName = "Canh Bắp Ngọt Nấm & Rau Muống Xào Tỏi",
                Description = "Bắp ngọt Mỹ hầm nước dùng ngọt lịm, đĩa rau muống xanh mướt giòn rụm.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Chay")
            });
        }
        else
        {
            var q1 = new List<IngredientQuery>
            {
                new(9, "ba chỉ", servings >= 4 ? 2 : 1),
                new(9, "trứng gà", 1),
                new(4, "rau muống", 1)
            };
            var items1 = MatchProductsForQueries(q1, products, batchLookup, prioritizePromotions);
            decimal cost1 = items1.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig1 = items1.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 1: Thịt Ba Chỉ Kho Tàu & Canh Rau Muống",
                DishName = "Thịt Ba Chỉ Kho Tàu & Canh Rau Muống",
                Description = "Thịt ba chỉ MeatDeli kho trứng mềm rục vàng óng, ăn kèm canh rau muống thanh mát.",
                TotalEstimatedCost = cost1,
                TotalSavedAmount = Math.Max(0, orig1 - cost1),
                Ingredients = items1,
                CookingTips = GenerateCookingTips("Kho Tàu")
            });

            var q2 = new List<IngredientQuery>
            {
                new(9, "đùi gà", servings >= 4 ? 2 : 1),
                new(4, "cải ngọt", 1),
                new(4, "hành lá", 1)
            };
            var items2 = MatchProductsForQueries(q2, products, batchLookup, prioritizePromotions);
            decimal cost2 = items2.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig2 = items2.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 2: Đùi Gà Rim Mắm & Canh Cải Ngọt (Tiết kiệm)",
                DishName = "Đùi Gà Rim & Canh Cải Ngọt",
                Description = "Đùi gà C.P rim mắm tỏi thơm lừng, canh cải ngọt VietGAP thanh nhẹ vừa túi tiền.",
                TotalEstimatedCost = cost2,
                TotalSavedAmount = Math.Max(0, orig2 - cost2),
                Ingredients = items2,
                CookingTips = GenerateCookingTips("Gà")
            });

            var q3 = new List<IngredientQuery>
            {
                new(9, "sườn non", servings >= 4 ? 2 : 1),
                new(4, "cà chua", 1),
                new(4, "mướp hương", 1)
            };
            var items3 = MatchProductsForQueries(q3, products, batchLookup, prioritizePromotions);
            decimal cost3 = items3.Sum(i => i.FinalPrice * i.QuantityToBuy);
            decimal orig3 = items3.Sum(i => i.RegularPrice * i.QuantityToBuy);
            options.Add(new MealOptionDto
            {
                OptionName = "Option 3: Sườn Non Xào Chua Ngọt & Canh Mướp",
                DishName = "Sườn Non Xào Chua Ngọt & Canh Mướp",
                Description = "Sườn non tươi C.P sốt sánh óng ánh, canh mướp ngọt lành giải nhiệt.",
                TotalEstimatedCost = cost3,
                TotalSavedAmount = Math.Max(0, orig3 - cost3),
                Ingredients = items3,
                CookingTips = GenerateCookingTips("Chua Ngọt")
            });
        }

        return options;
    }

    private static List<MealIngredientItemDto> MatchProductsForQueries(
        List<IngredientQuery> queries,
        List<Product> products,
        Dictionary<int, ProductBatchSummary> batchLookup,
        bool prioritizePromotions)
    {
        var result = new List<MealIngredientItemDto>();
        var selectedProductIds = new HashSet<int>();

        foreach (var q in queries)
        {
            var match = products
                .Where(p => !selectedProductIds.Contains(p.ProductId))
                .Where(p => !q.CategoryId.HasValue || p.CategoryId == q.CategoryId.Value)
                .Where(p => p.ProductName.Contains(q.Keyword, StringComparison.OrdinalIgnoreCase))
                .Where(p => string.IsNullOrEmpty(q.ExcludeWord) || !p.ProductName.Contains(q.ExcludeWord, StringComparison.OrdinalIgnoreCase))
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
                QuantityToBuy = q.DefaultQty,
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
        sb.AppendLine($"Dạ, theo yêu cầu của bạn (**{servings} người ăn**, ngân sách dự kiến **~{targetBudget:N0} đ**), mình đã cân đối và thiết kế **{options.Count} mâm cơm chuẩn vị theo cẩm nang Cookpad**:\n");

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            string icon = i == 0 ? "🍲" : (i == 1 ? "✨" : "🌿");
            sb.AppendLine($"{icon} **{opt.OptionName}** (~**{opt.TotalEstimatedCost:N0} đ**)");
            if (!string.IsNullOrWhiteSpace(opt.Description))
            {
                sb.AppendLine($"   *{opt.Description}*");
            }
        }

        sb.AppendLine("\n👉 Bạn có thể xem chi tiết nguyên liệu, giá tiền từng món và tùy ý thêm/bớt ở các thẻ mâm cơm bên dưới, sau đó bấm **\"Chọn mâm này\"** để nạp nhanh vào giỏ hàng nhé!");
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
            return "🥬 **Bí quyết luộc rau muống xanh mướt, giòn ngọt chuẩn đầu bếp (Cẩm nang Cookpad):**\n\n" +
                   "1. **Lửa lớn, nước ngập:** Nấu nước thật sôi bùng trên lửa lớn, lượng nước phải ngập hoàn toàn rau.\n" +
                   "2. **Thêm muối hạt:** Cho 1/2 thìa cà phê muối vào nước sôi để giữ sắc tố diệp lục tươi tắn.\n" +
                   "3. **Không đậy nắp vung:** Thả rau vào dùng đũa dìm ngập nước ngay, giữ lửa lớn và mở nắp để rau thoát nhiệt tốt.\n" +
                   "4. **Sốc nhiệt nước đá:** Luộc nhanh 2 - 3 phút, vớt ra thả ngay vào âu nước đá lạnh 2 phút rồi vớt ráo. Rau sẽ giòn sần sật suốt nhiều tiếng.\n\n" +
                   "💡 Nước luộc rau chỉ cần vắt thêm chút chanh tươi hoặc dầm sấu là có ngay bát canh thanh mát giải nhiệt!";
        }

        if (p.Contains("thịt bò") || p.Contains("không bị dai") || p.Contains("ướp bò"))
        {
            return "🥩 **Bí quyết ướp & xào thịt bò mềm mọng, không bị dai (Cẩm nang Cookpad):**\n\n" +
                   "1. **Cắt ngang thớ:** Nhìn rõ đường thớ thịt và dùng dao sắc thái lát mỏng ngang thớ. Thái dọc thớ thịt sẽ bị dai cứng.\n" +
                   "2. **Khóa ẩm bằng dầu & bột bắp:** Ướp thịt bò với 1 thìa cà phê dầu ăn, 1/2 thìa bột bắp (hoặc bột năng), tỏi băm, tiêu và hạt nêm. Tránh ướp nhiều muối hay nước mắm sớm vì muối sẽ hút nước khiến thịt bị khô.\n" +
                   "3. **Xào lửa cực đại:** Đun chảo thật nóng, cho dầu vào phi tỏi thơm rồi trút thịt bò vào đảo nhanh tay 1-2 phút vừa chín tái thì tắt bếp ngay.";
        }

        if (p.Contains("mực") || p.Contains("cá") || p.Contains("không bị tanh") || p.Contains("khử mùi"))
        {
            return "🐟 **Bí quyết khử tanh hải sản & giữ trọn độ tươi giòn (Cẩm nang Cookpad):**\n\n" +
                   "1. **Với cá tươi:** Rửa sạch màng đen trong bụng, xoa muối hạt và một chút rượu trắng hoặc nước cốt chanh lên thân cá trong 3 phút rồi rửa lại bằng nước sạch.\n" +
                   "2. **Với mực ống:** Bóc túi mực và mắt, khía vảy rồng mặt trong. Chần nhanh mực qua nước sôi có pha vài lát gừng đập dập trong 15 giây rồi vớt ra xả nước lạnh để mực giòn sần sật và không ra nước khi xào.";
        }

        if (p.Contains("sườn") || p.Contains("kho tàu"))
        {
            return "🍖 **Bí quyết ướp sườn & thịt kho tàu chuẩn vị Cookpad:**\n\n" +
                   "1. **Chần sơ nước sôi:** Chần sườn hoặc thịt qua nước sôi có thả nhánh gừng đập dập và củ hành tím để sạch bọt cặn và thơm thịt.\n" +
                   "2. **Thắng đường caramen chuẩn:** Cho 1 thìa dầu ăn và 1.5 thìa đường vào chảo, đun lửa nhỏ đến khi đường sôi bọt khí li ti và chuyển sang màu cánh gián thì cho hành tỏi vào phi thơm rồi đổ thịt vào đảo đều.\n" +
                   "3. **Kho với nước dừa tươi:** Nước dừa giúp thịt mềm rục, mỡ trong veo béo ngậy mà không hề ngấy.\n" +
                   "4. **Tỷ lệ sốt sườn chua ngọt vàng:** 2 thìa nước mắm : 2 thìa đường : 2 thìa giấm gạo : 1 thìa tương cà.";
        }

        return "👨‍🍳 **Mẹo ẩm thực hàng ngày từ Cookpad:**\n" +
               "Khi chế biến món ăn Việt hàng ngày, sự cân bằng ngũ vị (chua - cay - mặn - ngọt - đắng) và độ tươi ngon của nguyên liệu là yếu tố then chốt. " +
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

    private static string DetermineDishTheme(string prompt, decimal budget, int servings, List<ChatMessageDto>? history)
    {
        string p = prompt.ToLowerInvariant();

        if (p.Contains("lẩu") || p.Contains("lau")) return "Lẩu";
        if (p.Contains("sườn") || p.Contains("chua ngọt")) return "Sườn Non";
        if (p.Contains("thịt kho") || p.Contains("kho tàu") || p.Contains("ba chỉ") || p.Contains("ba rọi")) return "Thịt Kho Tàu";
        if (p.Contains("bò sốt vang") || p.Contains("sốt vang") || p.Contains("thịt bò")) return "Bò Úc";
        if (p.Contains("gà") || p.Contains("thịt gà") || p.Contains("cánh gà") || p.Contains("đùi gà") || p.Contains("ức gà")) return "Thịt Gà";
        if (p.Contains("chay") || p.Contains("thanh đạm")) return "Món Chay";
        if (p.Contains("trứng") || p.Contains("trứng chiên") || p.Contains("trứng rán")) return "Bữa Ăn Tiết Kiệm";
        if (p.Contains("mì") || p.Contains("mì tôm") || p.Contains("omachi") || p.Contains("hảo hảo")) return "Bữa Ăn Tiết Kiệm";
        if (p.Contains("cá hộp") || p.Contains("đồ hộp")) return "Bữa Ăn Tiết Kiệm";

        if (history != null && history.Any())
        {
            foreach (var msg in history.AsEnumerable().Reverse())
            {
                string hp = msg.Content.ToLowerInvariant();
                if (hp.Contains("lẩu")) return "Lẩu";
                if (hp.Contains("sườn")) return "Sườn Non";
                if (hp.Contains("thịt kho") || hp.Contains("kho tàu")) return "Thịt Kho Tàu";
                if (hp.Contains("sốt vang") || hp.Contains("thịt bò")) return "Bò Úc";
                if (hp.Contains("thịt gà") || hp.Contains("gà")) return "Thịt Gà";
            }
        }

        if (budget <= 60000m || (servings == 1 && budget <= 70000m) || p.Contains("tiết kiệm") || p.Contains("sinh viên") || p.Contains("bình dân") || p.Contains("giá rẻ"))
        {
            return "Bữa Ăn Tiết Kiệm";
        }

        return "Cơm Gia Đình Hàng Ngày";
    }

    private static string GenerateCookingTips(string theme)
    {
        if (theme.Contains("Lẩu"))
        {
            return "1. Rửa sạch rau nấm và cắt khúc vừa ăn. Ngâm nấm kim châm vào nước muối loãng 5 phút rồi để ráo.\n" +
                   "2. Nấu 1.8L nước sôi, cho ngô ngọt cắt khoanh vào đun 5-7 phút để nước dùng ngọt thanh tự nhiên.\n" +
                   "3. Nhúng thịt bò Úc Pacow và hải sản vừa chín tới để giữ vị ngọt mềm, ăn kèm nấm và rau xanh khi nước lẩu sôi bùng.";
        }
        if (theme.Contains("Kho Tàu"))
        {
            return "1. Thịt ba chỉ rửa sạch, chần sơ nước sôi rồi thái vuông 3-4cm. Ướp nước mắm cốt, hành tỏi băm, tiêu trong 20 phút.\n" +
                   "2. Thắng đường caramen màu cánh gián, cho thịt vào đảo săn đều rồi thêm nước kho nhỏ lửa liu riu 35 phút.\n" +
                   "3. Thả trứng luộc vào kho tiếp 15 phút đến khi thịt mềm rục, mỡ trong veo và nước sốt sánh mịn óng ánh.";
        }
        if (theme.Contains("Sốt Vang"))
        {
            return "1. Thịt bò Úc thái quân cờ, chần sơ rồi ướp hạt nêm, tiêu, tỏi băm và một chút gia vị trong 25 phút.\n" +
                   "2. Xào thịt bò với bơ tỏi cho xém cạnh, cho cà chua xào nhuyễn vào đảo đều, châm nước hầm nhỏ lửa 40 phút.\n" +
                   "3. Thêm khoai tây và cà rốt cắt khối vào hầm tiếp 15 phút đến khi rau củ mềm, nước sốt sánh sệt thì ăn kèm bánh mì giòn.";
        }
        if (theme.Contains("Chua Ngọt"))
        {
            return "1. Sườn non chặt miếng vừa ăn, chần qua nước sôi pha gừng để khử mùi và sạch bọt, để ráo.\n" +
                   "2. Rán sườn vàng xém 2 mặt. Pha sốt chua ngọt tỉ lệ vàng: 2 thìa nước mắm, 2 thìa đường, 2 thìa giấm, 1 thìa tương cà.\n" +
                   "3. Phi thơm hành tỏi, trút sườn và sốt vào đun lửa nhỏ đến khi sốt sền sệt quyện đều quanh từng miếng sườn.";
        }
        if (theme.Contains("Gà"))
        {
            return "1. Gà rửa sạch bằng nước muối loãng, chặt miếng vừa ăn và để thật ráo nước.\n" +
                   "2. Ướp gà với 1 thìa nước mắm, tỏi băm, hạt tiêu trong 15 phút cho ngấm đều.\n" +
                   "3. Rán sơ cho da gà xém vàng, sau đó đổ sốt mắm tỏi ớt vào đảo nhỏ lửa cho đến khi gà ngấm nước sốt keo lại.";
        }
        if (theme.Contains("Trứng"))
        {
            return "1. Đập trứng vào bát, thêm 1 thìa cà phê nước mắm, xíu tiêu và hành lá thái nhỏ. Đánh đều tay cho trứng nổi bọt mịn.\n" +
                   "2. Đun nóng chảo với dầu ăn, hạ lửa vừa rồi đổ trứng vào. Khi viền trứng phồng vàng thì khéo léo cuộn hoặc lật mặt để trứng xốp mềm không bị khô.\n" +
                   "3. Nấu nồi nước sôi, cho 1/2 thìa muối rồi thả rau muống vào luộc lửa lớn không đậy vung để rau xanh mướt.";
        }
        if (theme.Contains("Mì"))
        {
            return "1. Rửa sạch rau cải ngọt, cắt khúc 4-5cm. Cắt xúc xích thành các lát xéo đẹp mắt.\n" +
                   "2. Nấu 450ml nước sôi, cho gói gia vị mì và rau cải vào nấu 1 phút cho rau vừa chín tới.\n" +
                   "3. Thả vắt mì Omachi và xúc xích vào nấu thêm 2 phút, tắt bếp và thưởng thức ngay khi sợi mì còn dai giòn.";
        }
        if (theme.Contains("Cá Hộp"))
        {
            return "1. Rửa sạch dưa leo baby, ngâm nước muối loãng rồi thái lát tròn hoặc thanh dài giòn mát.\n" +
                   "2. Đun nóng 1 thìa dầu ăn, phi thơm đầu hành băm rồi đổ cá hộp sốt cà vào đun sôi nhỏ lửa 2-3 phút cho sốt sánh đậm đà.\n" +
                   "3. Rắc thêm chút tiêu xay, dùng nóng với cơm trắng và chấm kèm dưa leo baby giòn rụm.";
        }
        if (theme.Contains("Chay"))
        {
            return "1. Nấm đùi gà và nấm kim châm ngâm nước muối loãng 5 phút, rửa sạch, cắt miếng vừa ăn.\n" +
                   "2. Phi thơm sả tỏi với dầu đậu nành, cho nấm vào đảo nhanh tay ở lửa lớn để nấm giữ độ giòn ngọt thanh khiết.\n" +
                   "3. Nêm gia vị vừa miệng, dùng nóng cùng cơm trắng và canh cải thảo ngọt mát.";
        }

        return "1. Sơ chế sạch sẽ các nguyên liệu tươi sống và để ráo nước hoàn toàn trước khi nấu.\n" +
               "2. Cân bằng gia vị: Ưu tiên nêm nếm theo nguyên tắc vừa miệng, đun lửa vừa để giữ trọn vitamin.\n" +
               "3. Trình bày món ăn đẹp mắt và thưởng thức nóng sốt cùng gia đình để món ăn đạt hương vị trọn vẹn nhất.";
    }
}
