using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;

namespace SmartSupermarket.Backend.Features.AI.Cache.Implementations;

public class ConversationCache : IConversationCache
{
    private const string ContextKeyPrefix = "ai:conv:ctx:";
    private const string HistoryKeyPrefix = "ai:conv:hist:";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(2);
    private const int MaxHistoryTurnCount = 10;

    private readonly ICacheService _cacheService;
    private readonly ILogger<ConversationCache> _logger;

    public ConversationCache(ICacheService cacheService, ILogger<ConversationCache> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<AgentContextDto?> GetContextAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var key = $"{ContextKeyPrefix}{sessionId}";
        return await _cacheService.GetAsync<AgentContextDto>(key, cancellationToken);
    }

    public async Task SetContextAsync(
        string sessionId,
        AgentContextDto context,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || context == null)
        {
            return;
        }

        var key = $"{ContextKeyPrefix}{sessionId}";
        await _cacheService.SetAsync(key, context, slidingExpiration ?? DefaultTtl, cancellationToken);
    }

    public async Task<List<ChatMessageDto>?> GetHistoryAsync(
        string sessionId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new List<ChatMessageDto>();
        }

        var key = $"{HistoryKeyPrefix}{sessionId}";
        var list = await _cacheService.GetAsync<List<ChatMessageDto>>(key, cancellationToken);
        if (list == null || list.Count == 0)
        {
            return new List<ChatMessageDto>();
        }

        return list.TakeLast(Math.Max(1, limit)).ToList();
    }

    public async Task AppendMessageAsync(
        string sessionId,
        string role,
        string content,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var key = $"{HistoryKeyPrefix}{sessionId}";
        var list = await _cacheService.GetAsync<List<ChatMessageDto>>(key, cancellationToken) ?? new List<ChatMessageDto>();

        list.Add(new ChatMessageDto
        {
            Role = role,
            Content = content
        });

        if (list.Count > MaxHistoryTurnCount)
        {
            list.RemoveRange(0, list.Count - MaxHistoryTurnCount);
        }

        await _cacheService.SetAsync(key, list, slidingExpiration ?? DefaultTtl, cancellationToken);
    }

    public async Task ClearAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        await _cacheService.RemoveAsync($"{ContextKeyPrefix}{sessionId}", cancellationToken);
        await _cacheService.RemoveAsync($"{HistoryKeyPrefix}{sessionId}", cancellationToken);
    }
}
