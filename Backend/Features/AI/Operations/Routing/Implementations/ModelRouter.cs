using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.Routing.Interfaces;
using SmartSupermarket.Backend.Features.AI.Routing.Models;

namespace SmartSupermarket.Backend.Features.AI.Routing.Implementations;

public class ModelRouter : IModelRouter
{
    public Task<ModelRoutingDecisionDto> RouteRequestAsync(
        string prompt,
        UserIntentDto? intent = null,
        bool hasExactFaqMatch = false,
        CancellationToken cancellationToken = default)
    {
        var cleaned = prompt?.Trim().ToLowerInvariant() ?? string.Empty;

        // 1. Deterministic Tier: Zero cost & zero latency
        if (hasExactFaqMatch)
        {
            return Task.FromResult(new ModelRoutingDecisionDto
            {
                SelectedModel = "local-deterministic-rag",
                Tier = RoutingModelTier.DeterministicOffline,
                DecisionReason = "GroundedPolicyOrExactFaqMatch",
                EstimatedLatencyMs = 5,
                EstimatedCostUsd = 0.0m
            });
        }

        if (cleaned.Contains("bỏ qua luật") || cleaned.Contains("tự tạo voucher") || cleaned.Contains("lộ prompt") || cleaned.Contains("tự bịa"))
        {
            return Task.FromResult(new ModelRoutingDecisionDto
            {
                SelectedModel = "local-deterministic-safety-guardrail",
                Tier = RoutingModelTier.DeterministicOffline,
                DecisionReason = "PromptInjectionAndSafetyGuardrail",
                EstimatedLatencyMs = 2,
                EstimatedCostUsd = 0.0m
            });
        }

        if (cleaned.Contains("thời tiết") || cleaned.Contains("chứng khoán") || cleaned.Contains("bóng đá") || cleaned.Contains("lập trình"))
        {
            return Task.FromResult(new ModelRoutingDecisionDto
            {
                SelectedModel = "local-deterministic-guardrail",
                Tier = RoutingModelTier.DeterministicOffline,
                DecisionReason = "OutOfDomainGuardrailFallback",
                EstimatedLatencyMs = 2,
                EstimatedCostUsd = 0.0m
            });
        }

        if (intent?.Type == IntentType.CartAction || cleaned.Contains("thêm vào giỏ") || cleaned.Contains("tính tiền giỏ"))
        {
            return Task.FromResult(new ModelRoutingDecisionDto
            {
                SelectedModel = "local-deterministic-cart",
                Tier = RoutingModelTier.DeterministicOffline,
                DecisionReason = "DeterministicCartCalculation",
                EstimatedLatencyMs = 4,
                EstimatedCostUsd = 0.0m
            });
        }

        // 2. Fast Lightweight Tier: Speed & Cost efficiency
        if (intent?.Type == IntentType.Greeting || cleaned is "hi" or "hello" || cleaned.StartsWith("chào") || cleaned.StartsWith("xin chào") || cleaned.Contains("chào"))
        {
            return Task.FromResult(new ModelRoutingDecisionDto
            {
                SelectedModel = "groq-llama3-8b",
                Tier = RoutingModelTier.FastLightweight,
                DecisionReason = "FastLatencyGreeting",
                EstimatedLatencyMs = 280,
                EstimatedCostUsd = 0.00003m
            });
        }

        if (intent?.Type == IntentType.CulinaryAdvice || (cleaned.Contains("luộc sao") || cleaned.Contains("ướp sao") || cleaned.Contains("cách nấu")))
        {
            return Task.FromResult(new ModelRoutingDecisionDto
            {
                SelectedModel = "groq-llama3-8b",
                Tier = RoutingModelTier.FastLightweight,
                DecisionReason = "LightweightCulinaryTips",
                EstimatedLatencyMs = 380,
                EstimatedCostUsd = 0.00006m
            });
        }

        // 3. Strong Reasoning Tier: High-complexity planning and multi-tier retail tradeoff
        if (intent?.Type == IntentType.CategoryComparison || cleaned.Contains("so sánh") || cleaned.Contains("phân khúc nào"))
        {
            return Task.FromResult(new ModelRoutingDecisionDto
            {
                SelectedModel = "gemini-2.5-flash",
                Tier = RoutingModelTier.StrongReasoning,
                DecisionReason = "DeepRetailTradeoffComparison",
                EstimatedLatencyMs = 850,
                EstimatedCostUsd = 0.00019m
            });
        }

        // Default: Strong Reasoning for meal plan and budget constraint solving
        return Task.FromResult(new ModelRoutingDecisionDto
        {
            SelectedModel = "gemini-2.5-flash",
            Tier = RoutingModelTier.StrongReasoning,
            DecisionReason = "ComplexBudgetAndMealConstraintSolving",
            EstimatedLatencyMs = 920,
            EstimatedCostUsd = 0.00022m
        });
    }
}
