using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;

namespace SmartSupermarket.Backend.Features.AI.Services.Context;

public class ConversationContextService : IConversationContextService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConversationContextService> _logger;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);

    public ConversationContextService(IMemoryCache cache, ILogger<ConversationContextService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<AgentContextDto> GetOrCreateContextAsync(
        string sessionId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var key = GetContextCacheKey(sessionId);
        if (!_cache.TryGetValue<AgentContextDto>(key, out var context) || context == null)
        {
            context = new AgentContextDto
            {
                SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
                UserId = userId,
                Servings = 2,
                TargetBudget = 350000m
            };

            _cache.Set(key, context, new MemoryCacheEntryOptions
            {
                SlidingExpiration = DefaultSlidingExpiration
            });
        }

        return Task.FromResult(context);
    }

    public Task SaveContextAsync(AgentContextDto context, CancellationToken cancellationToken = default)
    {
        if (context == null || string.IsNullOrWhiteSpace(context.SessionId))
        {
            return Task.CompletedTask;
        }

        var key = GetContextCacheKey(context.SessionId);
        _cache.Set(key, context, new MemoryCacheEntryOptions
        {
            SlidingExpiration = DefaultSlidingExpiration
        });

        return Task.CompletedTask;
    }

    public Task AppendMessageAsync(
        string sessionId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(content))
        {
            return Task.CompletedTask;
        }

        var key = GetHistoryCacheKey(sessionId);
        if (!_cache.TryGetValue<List<ChatMessageDto>>(key, out var history) || history == null)
        {
            history = new List<ChatMessageDto>();
        }

        history.Add(new ChatMessageDto
        {
            Role = role,
            Content = content
        });

        // Giữ tối đa 10 lượt hội thoại gần nhất để tránh tràn context
        if (history.Count > 10)
        {
            history.RemoveRange(0, history.Count - 10);
        }

        _cache.Set(key, history, new MemoryCacheEntryOptions
        {
            SlidingExpiration = DefaultSlidingExpiration
        });

        return Task.CompletedTask;
    }

    public Task<List<ChatMessageDto>> GetHistoryAsync(
        string sessionId,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.FromResult(new List<ChatMessageDto>());
        }

        var key = GetHistoryCacheKey(sessionId);
        if (_cache.TryGetValue<List<ChatMessageDto>>(key, out var history) && history != null)
        {
            var result = history.TakeLast(Math.Max(1, limit)).ToList();
            return Task.FromResult(result);
        }

        return Task.FromResult(new List<ChatMessageDto>());
    }

    public Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _cache.Remove(GetHistoryCacheKey(sessionId));
            _cache.Remove(GetContextCacheKey(sessionId));
        }

        return Task.CompletedTask;
    }

    private static string GetContextCacheKey(string sessionId) => $"ai_context_{sessionId}";
    private static string GetHistoryCacheKey(string sessionId) => $"ai_history_{sessionId}";
}
