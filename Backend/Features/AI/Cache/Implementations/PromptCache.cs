using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Cache.Implementations;

public class PromptCache : IPromptCache
{
    private const string PromptKeyPrefix = "ai:prompt:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(12);

    private readonly ICacheService _cacheService;
    private readonly ILogger<PromptCache> _logger;

    public PromptCache(ICacheService cacheService, ILogger<PromptCache> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<string?> GetPromptAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var fullKey = $"{PromptKeyPrefix}{key}";
        return await _cacheService.GetAsync<string>(fullKey, cancellationToken);
    }

    public async Task SetPromptAsync(string key, string prompt, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        var fullKey = $"{PromptKeyPrefix}{key}";
        await _cacheService.SetAsync(fullKey, prompt, expiration ?? DefaultExpiration, cancellationToken);
    }

    public async Task<string> GetOrSetPromptAsync(
        string key,
        Func<CancellationToken, Task<string>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var fullKey = $"{PromptKeyPrefix}{key}";
        return await _cacheService.GetOrSetAsync(fullKey, factory, expiration ?? DefaultExpiration, cancellationToken);
    }

    public async Task InvalidatePromptAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var fullKey = $"{PromptKeyPrefix}{key}";
        await _cacheService.RemoveAsync(fullKey, cancellationToken);
    }
}
