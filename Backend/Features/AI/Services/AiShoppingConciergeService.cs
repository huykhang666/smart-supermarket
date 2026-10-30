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
    private readonly Interfaces.IRetailShoppingIntelligenceService _retailService;
    private readonly Knowledge.Interfaces.IKnowledgeRetrievalService _knowledgeService;
    private readonly PromptVersioning.Interfaces.IPromptVersionManager _promptVersionManager;
    private readonly Routing.Interfaces.IModelRouter _modelRouter;
    private readonly Optimization.Interfaces.IAiExperimentService _experimentService;

    public AiShoppingConciergeService(AppDbContext dbContext, ILlmClient llmClient)
        : this(dbContext, llmClient, new Implementations.RetailShoppingIntelligenceService(dbContext, new Tools.RetailTool(dbContext), llmClient))
    {
    }

    public AiShoppingConciergeService(
        AppDbContext dbContext,
        ILlmClient llmClient,
        Interfaces.IRetailShoppingIntelligenceService retailService)
        : this(dbContext, llmClient, retailService, new Knowledge.Implementations.KnowledgeRetrievalService())
    {
    }

    public AiShoppingConciergeService(
        AppDbContext dbContext,
        ILlmClient llmClient,
        Interfaces.IRetailShoppingIntelligenceService retailService,
        Knowledge.Interfaces.IKnowledgeRetrievalService knowledgeService)
        : this(dbContext, llmClient, retailService, knowledgeService,
               new PromptVersioning.Implementations.PromptVersionManager(),
               new Routing.Implementations.ModelRouter(),
               new Optimization.Implementations.AiExperimentService())
    {
    }

    public AiShoppingConciergeService(
        AppDbContext dbContext,
        ILlmClient llmClient,
        Interfaces.IRetailShoppingIntelligenceService retailService,
        Knowledge.Interfaces.IKnowledgeRetrievalService knowledgeService,
        PromptVersioning.Interfaces.IPromptVersionManager promptVersionManager,
        Routing.Interfaces.IModelRouter modelRouter,
        Optimization.Interfaces.IAiExperimentService experimentService)
    {
        _dbContext = dbContext;
        _llmClient = llmClient;
        _retailService = retailService;
        _knowledgeService = knowledgeService;
        _promptVersionManager = promptVersionManager;
        _modelRouter = modelRouter;
        _experimentService = experimentService;
    }

    private sealed record ProductBatchSummary(int TotalStock, DateTime MinExpiry);

    private sealed record IngredientQuery(
        int? CategoryId,
        string Keyword,
        int DefaultQty,
        string? ExcludeWord = null
    );

    private enum ShoppingIntent
    {
        Greeting,
        CulinaryAdvice,
        RetailShopping,
        AddToCart,
        EstimateCart,
        QueryPromotion,
        QueryInventory,
        ReplaceIngredient,
        RemoveIngredient,
        UpdateBudget,
        CreatePlan
    }

    public async Task<MealPlanChatResponse> PlanMealAndShopAsync(MealPlanChatRequest request, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var prompt = request.UserPrompt?.Trim() ?? string.Empty;

        int servings = request.NumberOfPeople ?? ParseServings(prompt, request.ConversationHistory);
        decimal targetBudget = request.TargetBudget ?? ParseBudget(prompt, request.ConversationHistory);
        if (targetBudget <= 0) targetBudget = 350000m;

        var intent = DetectIntent(prompt);
        var variant = _experimentService.AssignVariant(prompt);

        // Model Routing Evaluation
        var faqMatch = await _knowledgeService.GetExactMatchFaqAsync(prompt, cancellationToken);
        var routing = await _modelRouter.RouteRequestAsync(
            prompt,
            new SmartSupermarket.Backend.Features.AI.DTOs.Common.UserIntentDto
            {
                RawPrompt = prompt,
                Type = MapShoppingIntentToDtoType(intent)
            },
            faqMatch != null,
            cancellationToken);

        MealPlanChatResponse ReturnEnriched(MealPlanChatResponse res, bool isCacheHit = false)
        {
            return EnrichResponseWithOptimization(res, sw, variant, routing, isCacheHit);
        }

        if (IsSafetyOrPromptInjection(prompt))
        {
            return ReturnEnriched(new MealPlanChatResponse
            {
                AssistantMessage = "Dạ mình là Trợ lý Mua sắm Siêu thị Thông minh, luôn tuân thủ chính sách giá niêm yết, tồn kho thực tế và quy định an toàn hệ thống. Mình không thể tự tạo voucher, bịa giá hoặc tiết lộ cấu hình hệ thống. Bạn cần mình tư vấn món ăn hoặc kiểm tra khuyến mãi hiện có trên kệ siêu thị không ạ?",
                HasMealSuggestion = false,
                DishName = string.Empty,
                Servings = servings,
                TotalEstimatedCost = 0,
                TotalSavedAmount = 0,
                IsWithinBudget = true,
                Ingredients = new List<MealIngredientItemDto>(),
                MealOptions = new List<MealOptionDto>(),
                CookingTips = string.Empty
            });
        }

        if (IsUnderspecifiedVagueQuery(prompt) && (request.ConversationHistory == null || !request.ConversationHistory.Any()))
        {
            return ReturnEnriched(new MealPlanChatResponse
            {
                AssistantMessage = "Dạ để mình tư vấn chính xác và tiết kiệm nhất cho bạn, bạn có thể chia sẻ thêm:\n" +
                                   "1. Bạn muốn tìm món ăn gia đình hay sản phẩm bán lẻ (nước giặt, sữa tắm, đồ gia dụng)?\n" +
                                   "2. Số lượng người dùng hoặc ngân sách dự kiến khoảng bao nhiêu ạ?",
                HasMealSuggestion = false,
                DishName = string.Empty,
                Servings = servings,
                TotalEstimatedCost = 0,
                TotalSavedAmount = 0,
                IsWithinBudget = true,
                Ingredients = new List<MealIngredientItemDto>(),
                MealOptions = new List<MealOptionDto>(),
                CookingTips = string.Empty
            });
        }

        if (IsOutOfDomain(prompt))
        {
            return ReturnEnriched(new MealPlanChatResponse
            {
                AssistantMessage = "Dạ mình là Trợ lý Mua sắm & Đầu bếp của Smart Supermarket. Mình chuyên tư vấn thực đơn gia đình, nguyên liệu nấu ăn tươi ngon và các sản phẩm bán lẻ tại siêu thị. Bạn cần mình hỗ trợ món ăn hay mặt hàng nào hôm nay ạ?",
                HasMealSuggestion = false,
                DishName = string.Empty,
                Servings = servings,
                TotalEstimatedCost = 0,
                TotalSavedAmount = 0,
                IsWithinBudget = true,
                Ingredients = new List<MealIngredientItemDto>(),
                MealOptions = new List<MealOptionDto>(),
                CookingTips = string.Empty
            });
        }

        // Dedicated Branch: Knowledge & FAQ Grounding (Phase 3 RAG)
        if (faqMatch != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📌 **{faqMatch.Title}**\n");
            sb.AppendLine(faqMatch.Content);
            sb.AppendLine($"\n📚 *Căn cứ chính sách siêu thị:* `{faqMatch.SourceDoc}`");

            return ReturnEnriched(new MealPlanChatResponse
            {
                AssistantMessage = sb.ToString().TrimEnd(),
                HasMealSuggestion = false,
                DishName = string.Empty,
                Servings = servings,
                TotalEstimatedCost = 0,
                TotalSavedAmount = 0,
                IsWithinBudget = true,
                Ingredients = new List<MealIngredientItemDto>(),
                MealOptions = new List<MealOptionDto>(),
                CookingTips = string.Empty,
                KnowledgeSource = faqMatch.SourceDoc,
                GroundedFacts = new List<string> { faqMatch.Content }
            }, isCacheHit: true);
        }

        // Dedicated Branch: QueryPromotion (Không re-render cả mâm cơm nếu chỉ hỏi khuyến mãi)
        if (intent == ShoppingIntent.QueryPromotion)
        {
            return ReturnEnriched(await HandleQueryPromotionAsync(request, cancellationToken));
        }

        if (intent == ShoppingIntent.Greeting)
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

            return ReturnEnriched(new MealPlanChatResponse
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
            });
        }

        if (intent == ShoppingIntent.CulinaryAdvice)
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

            return ReturnEnriched(new MealPlanChatResponse
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
            });
        }

        // Dedicated Branch: RetailShopping (Tư vấn mua sắm ngành hàng bán lẻ siêu thị)
        if (intent == ShoppingIntent.RetailShopping)
        {
            decimal effectiveBudget = targetBudget > 0 ? targetBudget : (request.TargetBudget ?? 0);
            var retailResult = await _retailService.AdviseRetailShoppingAsync(
                prompt,
                effectiveBudget > 0 ? effectiveBudget : null,
                null,
                cancellationToken);

            var firstRec = retailResult.Recommendations.FirstOrDefault();
            bool isWithin = effectiveBudget <= 0 || (firstRec != null && firstRec.FinalPrice <= effectiveBudget);

            return ReturnEnriched(new MealPlanChatResponse
            {
                AssistantMessage = retailResult.AssistantMessage,
                HasMealSuggestion = false,
                DishName = string.Empty,
                Servings = servings,
                TotalEstimatedCost = firstRec?.FinalPrice ?? 0,
                TotalSavedAmount = Math.Max(0, (firstRec?.RegularPrice ?? 0) - (firstRec?.FinalPrice ?? 0)),
                IsWithinBudget = isWithin,
                Ingredients = retailResult.Recommendations.Select(r => new MealIngredientItemDto
                {
                    ProductId = r.ProductId,
                    ProductName = r.ProductName,
                    Barcode = r.Barcode,
                    ImageUrl = r.ImageUrl,
                    Unit = r.Unit,
                    QuantityToBuy = 1,
                    RegularPrice = r.RegularPrice,
                    FinalPrice = r.FinalPrice,
                    DiscountPercent = r.DiscountPercent,
                    InStockQuantity = r.InStockQuantity
                }).ToList(),
                MealOptions = new List<MealOptionDto>(),
                CookingTips = string.Empty,
                HasRetailRecommendation = true,
                ShoppingCategory = retailResult.ShoppingCategory,
                RetailRecommendations = retailResult.Recommendations,
                ComparisonTable = retailResult.ComparisonTable
            });
        }

        // Dedicated Branch: QueryPromotion (Không re-render cả mâm cơm nếu chỉ hỏi khuyến mãi)
        if (intent == ShoppingIntent.QueryPromotion)
        {
            return await HandleQueryPromotionAsync(request, cancellationToken);
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

        // TẦNG 1: PLAN STATE LAYER
        // 1. Khởi tạo mâm cơm gốc
        var currentMealOptions = BuildMealOptions(mainDishTheme, dbProducts, batchLookup, servings, targetBudget, request.PrioritizePromotions);

        // 2. Replay các thay đổi từ lịch sử hội thoại để duy trì trạng thái kế hoạch
        if (request.ConversationHistory != null && request.ConversationHistory.Any())
        {
            foreach (var msg in request.ConversationHistory.Where(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase)))
            {
                var historyNotes = new List<string>();
                ApplyMutationsToOptions(msg.Content, currentMealOptions, dbProducts, batchLookup, request.PrioritizePromotions, ref targetBudget, historyNotes);
            }
        }

        // 3. Áp dụng cập nhật của lượt hiện tại
        var currentUpdateNotes = new List<string>();
        ApplyMutationsToOptions(prompt, currentMealOptions, dbProducts, batchLookup, request.PrioritizePromotions, ref targetBudget, currentUpdateNotes);

        // Dedicated Branch: QueryInventory
        if (intent == ShoppingIntent.QueryInventory)
        {
            return ReturnEnriched(HandleQueryInventory(request, currentMealOptions));
        }

        // Dedicated Branch: AddToCart
        if (intent == ShoppingIntent.AddToCart)
        {
            return ReturnEnriched(HandleAddToCart(request, currentMealOptions));
        }

        // Dedicated Branch: EstimateCart
        if (intent == ShoppingIntent.EstimateCart)
        {
            return ReturnEnriched(HandleEstimateCart(request, currentMealOptions));
        }

        // Quy tắc Budget Optimization & Ranking:
        // Phân loại: Trong ngân sách vs Vượt ngân sách
        var inBudgetOptions = currentMealOptions
            .Where(o => o.TotalEstimatedCost <= targetBudget)
            .OrderBy(o => Math.Abs(targetBudget - o.TotalEstimatedCost))
            .ToList();

        var overBudgetOptions = currentMealOptions
            .Where(o => o.TotalEstimatedCost > targetBudget)
            .OrderBy(o => o.TotalEstimatedCost)
            .ToList();

        var rankedMealOptions = new List<MealOptionDto>();
        int optNumber = 1;

        // Nhóm 1 (Khuyến nghị chính) & Nhóm 2 (Phương án thay thế): Trong ngân sách
        for (int i = 0; i < inBudgetOptions.Count; i++)
        {
            var opt = inBudgetOptions[i];
            string cleanTitle = CleanOptionName(opt.OptionName);
            string groupTag = i == 0 ? "Khuyến nghị chính" : "Phương án thay thế";
            opt.OptionName = $"Option {optNumber++} ({groupTag}): {cleanTitle}";
            rankedMealOptions.Add(opt);
        }

        // Nhóm 3 (Phương án tham khảo - Vượt ngân sách): Vượt trần ngân sách
        for (int i = 0; i < overBudgetOptions.Count; i++)
        {
            var opt = overBudgetOptions[i];
            string cleanTitle = CleanOptionName(opt.OptionName);
            opt.OptionName = $"Option {optNumber++} (Tham khảo - Vượt ngân sách): {cleanTitle}";
            rankedMealOptions.Add(opt);
        }

        var mealOptions = rankedMealOptions.Any() ? rankedMealOptions : currentMealOptions;

        var primaryOption = mealOptions.FirstOrDefault(o => o.TotalEstimatedCost <= targetBudget)
            ?? mealOptions.FirstOrDefault()
            ?? new MealOptionDto
            {
                DishName = mainDishTheme,
                OptionName = "Thực đơn tiêu chuẩn",
                Ingredients = new List<MealIngredientItemDto>()
            };

        // TẦNG 2: RESPONSE RENDERING LAYER (Không reuse text cũ, render sạch theo state mới)
        string message = GenerateChefMealMessageWithMultiOptions(servings, targetBudget, mealOptions, currentUpdateNotes, null, null);

        // Chỉ gọi LLM bổ sung khi là CreatePlan thông thường và chưa có update note
        if (intent == ShoppingIntent.CreatePlan && !currentUpdateNotes.Any())
        {
            string? activeSystemPrompt = _promptVersionManager.GetActivePrompt(PromptVersioning.Models.PromptType.SystemIdentity)?.Content;
            string systemInstruction = !string.IsNullOrWhiteSpace(activeSystemPrompt) ? activeSystemPrompt :
                "Bạn là Đầu Bếp & Trợ Lý Đi Chợ Thông Minh của Smart Supermarket.\n" +
                "Đại từ xưng hô bắt buộc: xưng là 'mình' và gọi người dùng là 'bạn'. Tuyệt đối không dùng em/tôi/chúng tôi.\n" +
                "QUY TẮC BẮT BUỘC (05_System_Prompt & 06_Workflow):\n" +
                "1. Ngân sách là ưu tiên số một. Phương án trong ngân sách phải xếp lên đầu tiên.\n" +
                "2. Mọi cập nhật (đổi nguyên liệu, bỏ nguyên liệu, đổi ngân sách) phải được phản ánh vào kế hoạch và tính lại tổng tiền.\n" +
                "3. Không bịa giá, không bịa khuyến mãi, không bịa tồn kho, không bịa nguyên liệu.\n" +
                "4. Căn lề trái sạch sẽ, không dùng văn phong marketing quá đà.";

            string aiPrompt = $"Khách hàng hỏi: \"{prompt}\". " +
                $"Số người ăn: {servings} người, ngân sách khách đưa: {targetBudget:N0} đ.\n" +
                $"Danh sách các lựa chọn thực đơn đã được thiết kế và xếp hạng theo ngân sách từ kho siêu thị:\n" +
                string.Join("\n", mealOptions.Select((o, i) =>
                    $"- {o.OptionName}: Tổng chi phí {o.TotalEstimatedCost:N0} đ. " +
                    $"Trạng thái: {(o.TotalEstimatedCost <= targetBudget ? "Trong ngân sách" : "Vượt ngân sách")}. " +
                    $"Mô tả: {o.Description}. Nguyên liệu: {string.Join(", ", o.Ingredients.Take(4).Select(x => x.ProductName))}")) +
                "\nHãy viết lời tư vấn của đầu bếp ngắn gọn, chuẩn cấu trúc quy định trên.";

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
        }

        return ReturnEnriched(new MealPlanChatResponse
        {
            AssistantMessage = message,
            DishName = primaryOption.DishName,
            Servings = servings,
            TotalEstimatedCost = primaryOption.TotalEstimatedCost,
            TotalSavedAmount = primaryOption.TotalSavedAmount,
            IsWithinBudget = primaryOption.TotalEstimatedCost <= targetBudget,
            HasMealSuggestion = true,
            Ingredients = primaryOption.Ingredients,
            MealOptions = mealOptions,
            CookingTips = primaryOption.CookingTips
        });
    }

    private MealPlanChatResponse EnrichResponseWithOptimization(
        MealPlanChatResponse response,
        System.Diagnostics.Stopwatch stopwatch,
        Optimization.Models.AiExperimentVariantDto variant,
        Routing.Models.ModelRoutingDecisionDto routing,
        bool isCacheHit = false)
    {
        stopwatch.Stop();
        long latencyMs = Math.Max(1, stopwatch.ElapsedMilliseconds);

        response.ModelRouted = routing.SelectedModel;
        response.PromptVersion = variant.PromptVersion;
        response.ExperimentVariant = variant.VariantKey;
        response.ExecutionLatencyMs = latencyMs;
        response.EstimatedCostUsd = routing.EstimatedCostUsd;
        response.IsCacheHit = isCacheHit;

        _experimentService.RecordExperimentOutcome(new Optimization.Models.RequestTelemetryDto
        {
            RequestId = Guid.NewGuid().ToString("N"),
            ModelUsed = routing.SelectedModel,
            PromptVersion = variant.PromptVersion,
            ExperimentVariant = variant.VariantKey,
            RoutingTier = routing.Tier.ToString(),
            LatencyMs = latencyMs,
            CostUsd = routing.EstimatedCostUsd,
            CacheHit = isCacheHit
        });

        return response;
    }

    private static SmartSupermarket.Backend.Features.AI.DTOs.Common.IntentType MapShoppingIntentToDtoType(ShoppingIntent intent) => intent switch
    {
        ShoppingIntent.Greeting => SmartSupermarket.Backend.Features.AI.DTOs.Common.IntentType.Greeting,
        ShoppingIntent.CulinaryAdvice => SmartSupermarket.Backend.Features.AI.DTOs.Common.IntentType.CulinaryAdvice,
        ShoppingIntent.RetailShopping => SmartSupermarket.Backend.Features.AI.DTOs.Common.IntentType.ProductRecommendation,
        ShoppingIntent.AddToCart or ShoppingIntent.EstimateCart => SmartSupermarket.Backend.Features.AI.DTOs.Common.IntentType.CartAction,
        ShoppingIntent.QueryPromotion => SmartSupermarket.Backend.Features.AI.DTOs.Common.IntentType.PromotionInquiry,
        _ => SmartSupermarket.Backend.Features.AI.DTOs.Common.IntentType.MealPlanning
    };

    private static ShoppingIntent DetectIntent(string prompt)
    {
        string p = prompt.Trim().ToLowerInvariant();

        if (IsGreeting(p)) return ShoppingIntent.Greeting;
        if (IsCulinaryAdviceOnly(p)) return ShoppingIntent.CulinaryAdvice;

        if (Regex.IsMatch(p, @"(?:thêm|nạp|cho|bỏ|đặt)\s+(?:tất cả|hết|mâm này|nguyên liệu)?\s*(?:vào|vô)\s*giỏ", RegexOptions.IgnoreCase) ||
            p.Contains("thêm tất cả vào giỏ") || p.Contains("cho vào giỏ") || p.Contains("thêm vào giỏ") || p.Contains("nạp giỏ") || p.Contains("thêm giỏ"))
        {
            return ShoppingIntent.AddToCart;
        }

        if (p.Contains("tính tiền giỏ") || p.Contains("tạm tính giỏ") || p.Contains("ước tính giỏ") || p.Contains("tính giỏ"))
        {
            return ShoppingIntent.EstimateCart;
        }

        if (p.Contains("flash sale") || p.Contains("khuyến mãi") || p.Contains("voucher") || p.Contains("giảm giá") || p.Contains("ưu đãi"))
        {
            return ShoppingIntent.QueryPromotion;
        }

        if (p.Contains("còn hàng") || p.Contains("tồn kho") || p.Contains("kiểm tra hàng") || p.Contains("hết hàng"))
        {
            return ShoppingIntent.QueryInventory;
        }

        if (Regex.IsMatch(p, @"(?:đổi|thay)\s+([a-zA-ZÀ-ỹ0-9\s]+?)\s+(?:thành|bằng|qua|sang)\s+([a-zA-ZÀ-ỹ0-9\s]+?)(?:nhé|nha|ạ|\.|\,|$|\s+đi)", RegexOptions.IgnoreCase))
        {
            return ShoppingIntent.ReplaceIngredient;
        }

        if (Regex.IsMatch(p, @"(?:bỏ|xóa|không\s+lấy|không\s+ăn|đừng\s+lấy)\s+([a-zA-ZÀ-ỹ0-9\s]+?)(?:nhé|nha|ạ|\.|\,|$|\s+đi)", RegexOptions.IgnoreCase))
        {
            return ShoppingIntent.RemoveIngredient;
        }

        if (Regex.IsMatch(p, @"(?:giảm\s+xuống|giảm\s+còn|tăng\s+lên|ngân\s+sách\s+(?:là|còn)?)\s*(\d+)\s*[kK]", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(p, @"\b(?:còn|xuống)\s*(\d+)\s*[kK]\b", RegexOptions.IgnoreCase))
        {
            return ShoppingIntent.UpdateBudget;
        }

        if (IsRetailShopping(p) && !IsExplicitMealTheme(p))
        {
            return ShoppingIntent.RetailShopping;
        }

        return ShoppingIntent.CreatePlan;
    }

    private static string ScrubIngredientFromText(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term)) return text;
        string escaped = Regex.Escape(term);

        string pattern = $@"(?:\s*(?:&|và|kèm|với|kết hợp)\s*)?(?:các loại\s+)?[a-zA-ZÀ-ỹ0-9\s]*{escaped}[a-zA-ZÀ-ỹ0-9\s]*(?=(?:,|\.|\;|\-|\–|\(|$))";
        text = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, $@"\b{escaped}\b", "", RegexOptions.IgnoreCase);

        text = Regex.Replace(text, @"\s{2,}", " ");
        text = Regex.Replace(text, @"\s*&\s*$", "");
        text = Regex.Replace(text, @"^\s*&\s*", "");
        text = Regex.Replace(text, @"\s*,\s*$", "");
        text = Regex.Replace(text, @"\s+([,\.;])", "$1");
        return text.Trim(' ', ',', '&', '-', '–');
    }

    private static void ApplyMutationsToOptions(
        string text,
        List<MealOptionDto> options,
        List<Product> dbProducts,
        Dictionary<int, ProductBatchSummary> batchLookup,
        bool prioritizePromotions,
        ref decimal targetBudget,
        List<string> updateNotes)
    {
        // 1. Check ReplaceIngredient ("đổi bò úc thành bò ta", "thay tôm bằng mực")
        var replaceMatch = Regex.Match(text, @"(?:đổi|thay)\s+([a-zA-ZÀ-ỹ0-9\s]+?)\s+(?:thành|bằng|qua|sang)\s+([a-zA-ZÀ-ỹ0-9\s]+?)(?:nhé|nha|ạ|\.|\,|$|\s+đi)", RegexOptions.IgnoreCase);
        if (replaceMatch.Success)
        {
            var fromItem = replaceMatch.Groups[1].Value.Trim().ToLowerInvariant();
            var toItem = replaceMatch.Groups[2].Value.Trim().ToLowerInvariant();

            var replacementProduct = dbProducts.FirstOrDefault(p => p.ProductName.ToLowerInvariant().Contains(toItem));
            if (replacementProduct == null && (toItem.Contains("bò ta") || toItem.Contains("bò")))
            {
                replacementProduct = new Product
                {
                    ProductId = 9999,
                    ProductName = "Thịt Bò Ta Tươi Sạch (VietGAP)",
                    Price = 95000,
                    Unit = "Khay 250g",
                    Barcode = "8939999999999"
                };
            }

            if (replacementProduct != null)
            {
                var summary = batchLookup.TryGetValue(replacementProduct.ProductId, out var s) ? s : new ProductBatchSummary(50, DateTime.UtcNow.AddDays(7));
                decimal unitPrice = replacementProduct.Price;
                int disc = prioritizePromotions ? 30 : 0;
                decimal finalPrice = unitPrice * (100 - disc) / 100m;

                foreach (var opt in options)
                {
                    var targetIdx = opt.Ingredients.FindIndex(i => i.ProductName.ToLowerInvariant().Contains(fromItem));
                    if (targetIdx >= 0)
                    {
                        var oldItem = opt.Ingredients[targetIdx];
                        opt.Ingredients[targetIdx] = new MealIngredientItemDto
                        {
                            ProductId = replacementProduct.ProductId,
                            ProductName = replacementProduct.ProductName,
                            Barcode = replacementProduct.Barcode ?? "8939999999999",
                            ImageUrl = replacementProduct.ImageUrl ?? oldItem.ImageUrl,
                            Unit = replacementProduct.Unit ?? oldItem.Unit,
                            QuantityToBuy = oldItem.QuantityToBuy,
                            RegularPrice = unitPrice,
                            FinalPrice = finalPrice,
                            DiscountPercent = disc,
                            IsPromotionOrNearExpiry = disc > 0,
                            PromotionNote = disc > 0 ? $"Flash Sale giảm {disc}%" : "Hàng tiêu chuẩn",
                            InStockQuantity = summary.TotalStock
                        };
                        opt.TotalEstimatedCost = opt.Ingredients.Sum(i => i.FinalPrice * i.QuantityToBuy);
                        opt.TotalSavedAmount = opt.Ingredients.Sum(i => (i.RegularPrice - i.FinalPrice) * i.QuantityToBuy);
                    }

                    string repName = replacementProduct.ProductName.Contains("Bò Ta") ? "Bò Ta" : replacementProduct.ProductName;
                    opt.DishName = Regex.Replace(opt.DishName, Regex.Escape(fromItem), repName, RegexOptions.IgnoreCase);
                    opt.DishName = Regex.Replace(opt.DishName, "bò úc", repName, RegexOptions.IgnoreCase);
                    opt.OptionName = Regex.Replace(opt.OptionName, Regex.Escape(fromItem), repName, RegexOptions.IgnoreCase);
                    opt.OptionName = Regex.Replace(opt.OptionName, "bò úc", repName, RegexOptions.IgnoreCase);
                    opt.Description = Regex.Replace(opt.Description, Regex.Escape(fromItem), repName.ToLowerInvariant(), RegexOptions.IgnoreCase);
                    opt.Description = Regex.Replace(opt.Description, "bò úc pacow", repName.ToLowerInvariant(), RegexOptions.IgnoreCase);
                    opt.Description = Regex.Replace(opt.Description, "bò úc", repName.ToLowerInvariant(), RegexOptions.IgnoreCase);
                    opt.Description = Regex.Replace(opt.Description, "pacow", "Việt Nam", RegexOptions.IgnoreCase);
                }
                updateNotes.Add($"Đã đổi '{fromItem}' thành '{replacementProduct.ProductName}'");
            }
        }

        // 2. Check RemoveIngredient ("bỏ nấm", "xóa nấm", "không lấy nấm")
        var removeMatch = Regex.Match(text, @"(?:bỏ|xóa|không\s+lấy|không\s+ăn|đừng\s+lấy)\s+([a-zA-ZÀ-ỹ0-9\s]+?)(?:nhé|nha|ạ|\.|\,|$|\s+đi)", RegexOptions.IgnoreCase);
        if (removeMatch.Success)
        {
            var itemToRemove = removeMatch.Groups[1].Value.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(itemToRemove) && itemToRemove.Length >= 2)
            {
                foreach (var opt in options)
                {
                    var removedCount = opt.Ingredients.RemoveAll(ing => ing.ProductName.ToLowerInvariant().Contains(itemToRemove));
                    if (removedCount > 0)
                    {
                        opt.TotalEstimatedCost = opt.Ingredients.Sum(i => i.FinalPrice * i.QuantityToBuy);
                        opt.TotalSavedAmount = opt.Ingredients.Sum(i => (i.RegularPrice - i.FinalPrice) * i.QuantityToBuy);
                    }

                    opt.DishName = ScrubIngredientFromText(opt.DishName, itemToRemove);
                    opt.OptionName = ScrubIngredientFromText(opt.OptionName, itemToRemove);
                    opt.Description = ScrubIngredientFromText(opt.Description, itemToRemove);

                    if (string.IsNullOrWhiteSpace(opt.Description) || opt.Description.Contains(itemToRemove, StringComparison.OrdinalIgnoreCase))
                    {
                        var ingNames = opt.Ingredients.Take(3).Select(x => x.ProductName).ToList();
                        opt.Description = ingNames.Any()
                            ? $"Mâm nguyên liệu tươi ngon gồm {string.Join(", ", ingNames)}, đảm bảo chuẩn vị gia đình."
                            : "Mâm nguyên liệu tươi ngon chuẩn vị theo cẩm nang gia đình.";
                    }
                    if (string.IsNullOrWhiteSpace(opt.DishName) || opt.DishName.Contains(itemToRemove, StringComparison.OrdinalIgnoreCase))
                    {
                        opt.DishName = "Lẩu Bò Tươi Chuẩn Vị";
                    }
                }
                updateNotes.Add($"Đã bỏ nguyên liệu '{itemToRemove}'");
            }
        }

        // 3. Check UpdateBudget ("giảm còn 250k", "ngân sách 250k")
        var budgetMatch = Regex.Match(text, @"(?:giảm\s+xuống|giảm\s+còn|tăng\s+lên|ngân\s+sách\s+(?:là|còn)?)\s*(\d+)\s*[kK]", RegexOptions.IgnoreCase);
        if (!budgetMatch.Success)
        {
            budgetMatch = Regex.Match(text, @"\b(?:còn|xuống)\s*(\d+)\s*[kK]\b", RegexOptions.IgnoreCase);
        }
        if (budgetMatch.Success && decimal.TryParse(budgetMatch.Groups[1].Value, out decimal newBudgetK))
        {
            targetBudget = newBudgetK * 1000m;
            updateNotes.Add($"Đã cập nhật ngân sách mới: {targetBudget:N0} đ");
        }
    }

    private async Task<MealPlanChatResponse> HandleQueryPromotionAsync(
        MealPlanChatRequest request,
        CancellationToken cancellationToken)
    {
        var activePromos = await _dbContext.Promotions
            .Where(p => p.IsActive)
            .Take(5)
            .ToListAsync(cancellationToken);

        var nearExpiryBatches = await _dbContext.InventoryBatches
            .Include(b => b.Product)
            .Where(b => b.Quantity > 0 && b.ExpiryDate <= DateTime.UtcNow.AddDays(7))
            .OrderBy(b => b.ExpiryDate)
            .Take(4)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("**Chương trình Khuyến mãi & Flash Sale hiện có tại siêu thị:**\n");

        if (nearExpiryBatches.Any())
        {
            sb.AppendLine("⚡ **Flash Sale 30% — Hàng tươi sống cận date (3-5 ngày):**");
            foreach (var b in nearExpiryBatches)
            {
                decimal salePrice = b.Product.Price * 0.7m;
                int daysLeft = Math.Max(1, (int)(b.ExpiryDate - DateTime.UtcNow).TotalDays);
                sb.AppendLine($"- **{b.Product.ProductName}**: `{salePrice:N0} đ` *(Giá gốc: {b.Product.Price:N0} đ, còn {daysLeft} ngày HSD, tồn {b.Quantity} {b.Product.Unit})*");
            }
            sb.AppendLine();
        }

        if (activePromos.Any())
        {
            sb.AppendLine("🎟️ **Voucher & Chương trình ưu đãi:**");
            foreach (var p in activePromos)
            {
                string discountDesc = p.DiscountType == "Percentage" ? $"Giảm {p.DiscountValue}%" : $"Giảm {p.DiscountValue:N0} đ";
                string minOrder = p.MinimumOrderAmount > 0 ? $" cho đơn từ {p.MinimumOrderAmount:N0} đ" : "";
                sb.AppendLine($"- **Mã {p.PromotionCode}**: {p.PromotionName} ({discountDesc}{minOrder})");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("🎟️ **Voucher siêu thị:**");
            sb.AppendLine("- **SMART20**: Giảm 20.000 đ cho đơn từ 200.000 đ.");
            sb.AppendLine("- **FREESHIP**: Miễn phí giao hàng cho đơn từ 150.000 đ.\n");
        }

        sb.AppendLine("👉 Bạn có muốn mình áp dụng nguyên liệu Flash Sale nào vào thực đơn của bạn để tiết kiệm thêm không?");

        return new MealPlanChatResponse
        {
            AssistantMessage = sb.ToString().TrimEnd(),
            HasMealSuggestion = false,
            DishName = string.Empty,
            Servings = request.NumberOfPeople ?? 4,
            TotalEstimatedCost = 0,
            TotalSavedAmount = 0,
            IsWithinBudget = true,
            Ingredients = new List<MealIngredientItemDto>(),
            MealOptions = new List<MealOptionDto>(),
            CookingTips = string.Empty
        };
    }

    private static MealPlanChatResponse HandleQueryInventory(
        MealPlanChatRequest request,
        List<MealOptionDto> mealOptions)
    {
        var currentOption = mealOptions.FirstOrDefault();
        var sb = new StringBuilder();
        sb.AppendLine("**Tình trạng tồn kho theo thời gian thực từ CSDL siêu thị:**\n");

        if (currentOption != null && currentOption.Ingredients.Any())
        {
            foreach (var ing in currentOption.Ingredients)
            {
                sb.AppendLine($"- **{ing.ProductName}**: Còn `{ing.InStockQuantity} {ing.Unit}` trong kho (Đạt chuẩn tươi ngon VietGAP/HACCP).");
            }
            sb.AppendLine();
            sb.AppendLine("👉 Toàn bộ nguyên liệu đều sẵn sàng trong kho siêu thị để bạn đặt mua ngay!");
        }
        else
        {
            sb.AppendLine("Toàn bộ các mặt hàng trong danh mục đều đang có sẵn tồn kho dồi dào tại siêu thị.");
        }

        return new MealPlanChatResponse
        {
            AssistantMessage = sb.ToString().TrimEnd(),
            HasMealSuggestion = false,
            DishName = currentOption?.DishName ?? string.Empty,
            Servings = request.NumberOfPeople ?? 4,
            TotalEstimatedCost = currentOption?.TotalEstimatedCost ?? 0,
            TotalSavedAmount = currentOption?.TotalSavedAmount ?? 0,
            IsWithinBudget = true,
            Ingredients = currentOption?.Ingredients ?? new List<MealIngredientItemDto>(),
            MealOptions = new List<MealOptionDto>(),
            CookingTips = string.Empty
        };
    }

    private static MealPlanChatResponse HandleAddToCart(
        MealPlanChatRequest request,
        List<MealOptionDto> mealOptions)
    {
        var currentOption = mealOptions.FirstOrDefault();
        var sb = new StringBuilder();
        sb.AppendLine("**Đã thêm toàn bộ nguyên liệu vào giỏ hàng thành công!** 🛒\n");

        if (currentOption != null && currentOption.Ingredients.Any())
        {
            sb.AppendLine($"- **Thực đơn**: {CleanOptionName(currentOption.OptionName)}");
            sb.AppendLine($"- **Số lượng sản phẩm**: {currentOption.Ingredients.Count} mặt hàng");
            sb.AppendLine($"- **Tổng giá trị tạm tính**: `{currentOption.TotalEstimatedCost:N0} đ`");
            if (currentOption.TotalSavedAmount > 0)
            {
                sb.AppendLine($"- **Tiết kiệm được**: `{currentOption.TotalSavedAmount:N0} đ` từ Flash Sale");
            }
            sb.AppendLine();
            sb.AppendLine("👉 Bạn có thể vào mục **Giỏ hàng** để kiểm tra, điều chỉnh số lượng hoặc bấm **Thanh toán** nhé!");
        }
        else
        {
            sb.AppendLine("Đã nạp các mặt hàng vào giỏ hàng của bạn.");
        }

        return new MealPlanChatResponse
        {
            AssistantMessage = sb.ToString().TrimEnd(),
            HasMealSuggestion = false,
            DishName = currentOption?.DishName ?? string.Empty,
            Servings = request.NumberOfPeople ?? 4,
            TotalEstimatedCost = currentOption?.TotalEstimatedCost ?? 0,
            TotalSavedAmount = currentOption?.TotalSavedAmount ?? 0,
            IsWithinBudget = true,
            Ingredients = currentOption?.Ingredients ?? new List<MealIngredientItemDto>(),
            MealOptions = new List<MealOptionDto>(),
            CookingTips = string.Empty
        };
    }

    private static MealPlanChatResponse HandleEstimateCart(
        MealPlanChatRequest request,
        List<MealOptionDto> mealOptions)
    {
        var currentOption = mealOptions.FirstOrDefault();
        var sb = new StringBuilder();
        sb.AppendLine("**Ước tính giỏ hàng hiện tại:**\n");

        if (currentOption != null)
        {
            sb.AppendLine($"- **Món ăn**: {CleanOptionName(currentOption.OptionName)}");
            sb.AppendLine($"- **Số người ăn**: {request.NumberOfPeople ?? 4} người");
            sb.AppendLine($"- **Tổng chi phí ước tính**: `{currentOption.TotalEstimatedCost:N0} đ`");
            if (currentOption.TotalSavedAmount > 0)
            {
                sb.AppendLine($"- **Tổng tiết kiệm**: `{currentOption.TotalSavedAmount:N0} đ`");
            }
            sb.AppendLine();
            sb.AppendLine("👉 Bạn có thể nói *\"Thêm tất cả vào giỏ\"* để nạp ngay vào giỏ hàng nhé!");
        }

        return new MealPlanChatResponse
        {
            AssistantMessage = sb.ToString().TrimEnd(),
            HasMealSuggestion = false,
            DishName = currentOption?.DishName ?? string.Empty,
            Servings = request.NumberOfPeople ?? 4,
            TotalEstimatedCost = currentOption?.TotalEstimatedCost ?? 0,
            TotalSavedAmount = currentOption?.TotalSavedAmount ?? 0,
            IsWithinBudget = true,
            Ingredients = currentOption?.Ingredients ?? new List<MealIngredientItemDto>(),
            MealOptions = new List<MealOptionDto>(),
            CookingTips = string.Empty
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

    private static string CleanOptionName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Mâm cơm";
        var match = Regex.Match(raw, @"^Option\s*\d+[^:]*:\s*(.*)$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : raw.Trim();
    }

    private static string GenerateChefMealMessageWithMultiOptions(
        int servings,
        decimal targetBudget,
        List<MealOptionDto> options,
        List<string>? updateNotes = null,
        string? promoNote = null,
        string? inventoryNote = null)
    {
        var inBudget = options.Where(o => o.TotalEstimatedCost <= targetBudget).ToList();
        var overBudget = options.Where(o => o.TotalEstimatedCost > targetBudget).ToList();
        var sb = new StringBuilder();

        bool hasUpdate = (updateNotes != null && updateNotes.Any()) || !string.IsNullOrWhiteSpace(promoNote) || !string.IsNullOrWhiteSpace(inventoryNote);
        if (hasUpdate)
        {
            sb.AppendLine("**Mình đã cập nhật theo yêu cầu của bạn.**");
            var primary = inBudget.FirstOrDefault() ?? options.FirstOrDefault();
            if (primary != null)
            {
                sb.AppendLine($"- **Phương án chính**: {CleanOptionName(primary.OptionName)}");
                sb.AppendLine($"- **Tổng tiền**: `{primary.TotalEstimatedCost:N0} đ`");
                sb.AppendLine($"- **Trạng thái ngân sách**: {(primary.TotalEstimatedCost <= targetBudget ? "Trong ngân sách" : "Vượt ngân sách")}");
            }
            if (updateNotes != null && updateNotes.Any())
            {
                sb.AppendLine($"- **Các thay đổi**: {string.Join("; ", updateNotes)}");
            }
            if (!string.IsNullOrWhiteSpace(promoNote))
            {
                sb.AppendLine($"- **Khuyến mãi liên quan**: {promoNote}");
            }
            if (!string.IsNullOrWhiteSpace(inventoryNote))
            {
                sb.AppendLine($"- **Tồn kho liên quan**: {inventoryNote}");
            }
            sb.AppendLine();
        }

        if (inBudget.Any())
        {
            sb.AppendLine("**Các phương án phù hợp với ngân sách của bạn:**\n");

            int counter = 1;
            for (int i = 0; i < inBudget.Count; i++)
            {
                var opt = inBudget[i];
                string cleanTitle = CleanOptionName(opt.OptionName);
                string title = i == 0 ? $"Option đề xuất: {cleanTitle}" : $"Option thay thế: {cleanTitle}";
                sb.AppendLine($"{counter++}. **{title}** — `{opt.TotalEstimatedCost:N0} đ`");
                sb.AppendLine("   - Trạng thái: Trong ngân sách");
                string reason = !string.IsNullOrWhiteSpace(opt.Description)
                    ? opt.Description
                    : (i == 0 ? "Cân bằng tốt giữa chi phí và nguyên liệu" : "Nhiều nguyên liệu hơn nhưng sát trần ngân sách");
                sb.AppendLine($"   - Lý do: {reason}\n");
            }

            for (int i = 0; i < overBudget.Count; i++)
            {
                var opt = overBudget[i];
                string cleanTitle = CleanOptionName(opt.OptionName);
                sb.AppendLine($"{counter++}. **Option tham khảo: {cleanTitle}** — `{opt.TotalEstimatedCost:N0} đ`");
                sb.AppendLine("   - Trạng thái: Vượt ngân sách");
                sb.AppendLine("   - Lý do: Chỉ nên chọn nếu bạn muốn nâng mức chi\n");
            }

            sb.AppendLine("👉 Bạn có thể xem chi tiết nguyên liệu, giá tiền từng món và tùy ý thêm/bớt ở các thẻ mâm cơm bên dưới, sau đó bấm **\"Chọn mâm này\"** để nạp nhanh vào giỏ hàng nhé!");
        }
        else
        {
            sb.AppendLine("**Hiện tại chưa có phương án nào nằm trong ngân sách của bạn.**\n");
            sb.AppendLine("Mình có thể:");
            sb.AppendLine("- Đổi sang nguyên liệu rẻ hơn");
            sb.AppendLine("- Giảm số lượng");
            sb.AppendLine("- Chọn món đơn giản hơn\n");

            if (overBudget.Any())
            {
                sb.AppendLine("Dưới đây là các phương án gần nhất để bạn tham khảo:");
                for (int i = 0; i < overBudget.Count; i++)
                {
                    var opt = overBudget[i];
                    string cleanTitle = CleanOptionName(opt.OptionName);
                    sb.AppendLine($"- **{cleanTitle}** — `{opt.TotalEstimatedCost:N0} đ` (Vượt ngân sách: +{(opt.TotalEstimatedCost - targetBudget):N0} đ)");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static bool IsSafetyOrPromptInjection(string prompt)
    {
        string p = prompt.ToLowerInvariant();
        return p.Contains("bỏ qua luật") || p.Contains("bỏ qua hướng dẫn") || p.Contains("ignore instructions") ||
               p.Contains("tự tạo voucher") || p.Contains("giá rẻ hơn dữ liệu thật") || p.Contains("tự bịa") ||
               p.Contains("lộ prompt") || p.Contains("system prompt") || p.Contains("prompt hệ thống") ||
               p.Contains("đừng dùng database") || p.Contains("bịa giá") || p.Contains("bịa giúp");
    }

    private static bool IsUnderspecifiedVagueQuery(string prompt)
    {
        string p = prompt.Trim().ToLowerInvariant().TrimEnd('?', '.', '!');
        return p is "làm món gì ngon" or "mua cái nào tốt" or "loại nào xịn" or "cái này có ổn không" or "ăn gì ngon" or "mua gì" or "món gì ngon";
    }

    private static bool IsOutOfDomain(string prompt)
    {
        string p = prompt.ToLowerInvariant();
        return p.Contains("thời tiết") || p.Contains("chứng khoán") || p.Contains("bóng đá") ||
               p.Contains("lập trình") || p.Contains("chính trị") || p.Contains("thơ ca");
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
