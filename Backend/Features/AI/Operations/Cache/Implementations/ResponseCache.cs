using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Cache.Implementations;

public class ResponseCache : IResponseCache
{
    private const string ResponseKeyPrefix = "ai:resp:";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    private readonly ICacheService _cacheService;
    private readonly ILogger<ResponseCache> _logger;

    public ResponseCache(ICacheService cacheService, ILogger<ResponseCache> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<string?> GetResponseAsync(
        string prompt,
        string? systemInstruction = null,
        bool requireJson = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        var key = ComputeHashKey(prompt, systemInstruction, requireJson);
        var cached = await _cacheService.GetAsync<string>(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            _logger.LogDebug("[ResponseCache] Cache hit for LLM response with key: {Key}", key);
            return cached;
        }

        return null;
    }

    public async Task SetResponseAsync(
        string prompt,
        string response,
        string? systemInstruction = null,
        bool requireJson = false,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(response))
        {
            return;
        }

        var key = ComputeHashKey(prompt, systemInstruction, requireJson);
        await _cacheService.SetAsync(key, response, expiration ?? DefaultTtl, cancellationToken);
        _logger.LogDebug("[ResponseCache] Cached LLM response with key: {Key}", key);
    }

    public Task ClearResponseCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static string ComputeHashKey(string prompt, string? systemInstruction, bool requireJson)
    {
        var rawKey = $"p={prompt}|sys={systemInstruction ?? string.Empty}|json={requireJson}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        var hex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"{ResponseKeyPrefix}{hex}";
    }
}
