using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;

namespace SmartSupermarket.Backend.Features.AI.Services.Context;

public class ConversationContextService : IConversationContextService
{
    private readonly IConversationCache _conversationCache;
    private readonly ILogger<ConversationContextService> _logger;

    public ConversationContextService(IConversationCache conversationCache, ILogger<ConversationContextService> logger)
    {
        _conversationCache = conversationCache;
        _logger = logger;
    }

    public async Task<AgentContextDto> GetOrCreateContextAsync(
        string sessionId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var context = await _conversationCache.GetContextAsync(sessionId, cancellationToken);
        if (context == null)
        {
            context = new AgentContextDto
            {
                SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
                UserId = userId,
                Servings = 2,
                TargetBudget = 350000m
            };

            await _conversationCache.SetContextAsync(context.SessionId, context, cancellationToken: cancellationToken);
        }

        return context;
    }

    public async Task SaveContextAsync(AgentContextDto context, CancellationToken cancellationToken = default)
    {
        if (context == null || string.IsNullOrWhiteSpace(context.SessionId))
        {
            return;
        }

        await _conversationCache.SetContextAsync(context.SessionId, context, cancellationToken: cancellationToken);
    }

    public async Task AppendMessageAsync(
        string sessionId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        await _conversationCache.AppendMessageAsync(sessionId, role, content, cancellationToken: cancellationToken);
    }

    public async Task<List<ChatMessageDto>> GetHistoryAsync(
        string sessionId,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new List<ChatMessageDto>();
        }

        var history = await _conversationCache.GetHistoryAsync(sessionId, limit, cancellationToken);
        return history ?? new List<ChatMessageDto>();
    }

    public async Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _conversationCache.ClearAsync(sessionId, cancellationToken);
        }
    }
}
