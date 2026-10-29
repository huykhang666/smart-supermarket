using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Cache.Implementations;

public class KnowledgeCache : IKnowledgeCache
{
    private const string KnowledgeKeyPrefix = "ai:know:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    private readonly ICacheService _cacheService;
    private readonly ILogger<KnowledgeCache> _logger;

    public KnowledgeCache(ICacheService cacheService, ILogger<KnowledgeCache> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<T?> GetKnowledgeAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return default;
        }

        var fullKey = $"{KnowledgeKeyPrefix}{key}";
        return await _cacheService.GetAsync<T>(fullKey, cancellationToken);
    }

    public async Task SetKnowledgeAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || value == null)
        {
            return;
        }

        var fullKey = $"{KnowledgeKeyPrefix}{key}";
        await _cacheService.SetAsync(fullKey, value, expiration ?? DefaultExpiration, cancellationToken);
    }

    public async Task<T> GetOrSetKnowledgeAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var fullKey = $"{KnowledgeKeyPrefix}{key}";
        return await _cacheService.GetOrSetAsync(fullKey, factory, expiration ?? DefaultExpiration, cancellationToken);
    }

    public async Task RemoveKnowledgeAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var fullKey = $"{KnowledgeKeyPrefix}{key}";
        await _cacheService.RemoveAsync(fullKey, cancellationToken);
    }
}
