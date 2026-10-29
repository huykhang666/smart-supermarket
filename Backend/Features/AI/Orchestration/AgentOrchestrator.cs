using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Dispatcher;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;
using SmartSupermarket.Backend.Features.AI.Monitoring.Interfaces;
using SmartSupermarket.Backend.Features.AI.Services.Context;
using SmartSupermarket.Backend.Features.AI.Services.Intent;
using SmartSupermarket.Backend.Features.AI.Services.Planning;
using SmartSupermarket.Backend.Features.AI.Services.Prompt;
using SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

namespace SmartSupermarket.Backend.Features.AI.Orchestration;

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IConversationContextService _contextService;
    private readonly IIntentAnalyzer _intentAnalyzer;
    private readonly IPlanner _planner;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILLMGateway _llmGateway;
    private readonly IToolDispatcher _toolDispatcher;
    private readonly IAiMonitoringService? _monitoringService;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IConversationContextService contextService,
        IIntentAnalyzer intentAnalyzer,
        IPlanner planner,
        IPromptBuilder promptBuilder,
        ILLMGateway llmGateway,
        IToolDispatcher toolDispatcher,
        ILogger<AgentOrchestrator> logger,
        IAiMonitoringService? monitoringService = null)
    {
        _contextService = contextService;
        _intentAnalyzer = intentAnalyzer;
        _planner = planner;
        _promptBuilder = promptBuilder;
        _llmGateway = llmGateway;
        _toolDispatcher = toolDispatcher;
        _logger = logger;
        _monitoringService = monitoringService;
    }

    public async Task<AiChatResponseDto> DispatchAsync(
        AiChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var rawPrompt = request.Message?.Trim() ?? string.Empty;
        var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId;

        _logger.LogInformation("[AgentOrchestrator] Tiếp nhận tin nhắn phiên {SessionId}: {Prompt}", sessionId, rawPrompt);

        try
        {
            // 1. Quản lý Context phiên hội thoại
            var context = await _contextService.GetOrCreateContextAsync(sessionId, null, cancellationToken);
            context.UserPrompt = rawPrompt;
            if (request.NumberOfPeople.HasValue) context.Servings = request.NumberOfPeople.Value;
            if (request.TargetBudget.HasValue) context.TargetBudget = request.TargetBudget.Value;

            // Lưu tin nhắn người dùng vào lịch sử phiên
            await _contextService.AppendMessageAsync(sessionId, "user", rawPrompt, cancellationToken);

            // 2. Phân tích Intent
            var intent = await _intentAnalyzer.AnalyzeIntentAsync(rawPrompt, cancellationToken);
            if (intent.Servings.HasValue) context.Servings = intent.Servings.Value;
            if (intent.TargetBudget.HasValue) context.TargetBudget = intent.TargetBudget.Value;
            context.Intent = intent.Type.ToString();

            // 3. Lập kế hoạch thực thi
            var plan = await _planner.CreatePlanAsync(intent, context, cancellationToken);
            _logger.LogInformation("[AgentOrchestrator] Tạo kế hoạch {PlanId} cho intent {IntentType} với {StepCount} bước",
                plan.PlanId, plan.IntentType, plan.Steps.Count);

            // 4. Xây dựng Prompt đa tầng
            var systemPrompt = await _promptBuilder.BuildSystemPromptAsync("shopping", cancellationToken);
            var historyList = await _contextService.GetHistoryAsync(sessionId, 5, cancellationToken);
            var historyText = FormatHistory(historyList);
            var fullUserPrompt = await _promptBuilder.BuildUserPromptWithContextAsync(rawPrompt, historyText, null, cancellationToken);

            // 5. Sinh phản hồi qua LLM Gateway
            var assistantReply = await _llmGateway.GenerateTextAsync(fullUserPrompt, systemPrompt, cancellationToken);

            // Lưu phản hồi AI vào lịch sử phiên
            await _contextService.AppendMessageAsync(sessionId, "assistant", assistantReply, cancellationToken);
            await _contextService.SaveContextAsync(context, cancellationToken);

            // 6. Đóng gói phản hồi DTO
            var response = new AiChatResponseDto
            {
                Reply = assistantReply,
                SessionId = sessionId,
                HasMealSuggestion = intent.Type == IntentType.MealPlanning,
                Servings = context.Servings,
                TotalEstimatedPrice = context.TargetBudget,
                AgentUsed = "ShoppingAgent",
                SourceProvider = _llmGateway.ActiveProvider
            };

            // Nếu là MealPlanning, đính kèm các thẻ mâm cơm mẫu chuẩn Cookpad
            if (response.HasMealSuggestion)
            {
                AttachDefaultMealOptionCards(response, context);
            }

            sw.Stop();
            _monitoringService?.RecordRequest("ShoppingAgent", sw.Elapsed.TotalMilliseconds, true);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _monitoringService?.RecordRequest("ShoppingAgent", sw.Elapsed.TotalMilliseconds, false, error: ex.Message);
            throw;
        }
    }

    private static string FormatHistory(List<ChatMessageDto> history)
    {
        if (history == null || history.Count == 0) return string.Empty;
        var lines = new List<string>();
        foreach (var msg in history)
        {
            var roleLabel = msg.Role == "user" ? "Khách hàng" : "AI";
            lines.Add($"{roleLabel}: {msg.Content}");
        }
        return string.Join("\n", lines);
    }

    private static void AttachDefaultMealOptionCards(AiChatResponseDto response, AgentContextDto context)
    {
        response.OptionCards = new List<AiChatOptionCardDto>
        {
            new()
            {
                OptionId = 1,
                Title = "Cơm Trứng Chiên & Canh Rau Muống Luộc",
                Subtitle = "Trứng chiên xốp vàng, canh rau muống dầm chanh thanh mát",
                TotalPrice = 30800,
                SavedAmount = 5000,
                ItemCount = 3
            },
            new()
            {
                OptionId = 2,
                Title = "Mì Omachi Xúc Xích & Cải Ngọt Siêu Tốc",
                Subtitle = "Chế biến nhanh trong 5 phút, tiện lợi cho sinh viên",
                TotalPrice = 44300,
                SavedAmount = 3000,
                ItemCount = 3
            },
            new()
            {
                OptionId = 3,
                Title = "Cá Hộp Ba Cô Gái & Dưa Leo Baby",
                Subtitle = "Cá nục sốt cà đậm đà đưa cơm ăn kèm dưa giòn mát",
                TotalPrice = 28900,
                SavedAmount = 4500,
                ItemCount = 3
            }
        };
    }
}
