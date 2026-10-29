using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

namespace SmartSupermarket.Backend.Features.AI.Cache.Implementations;

public class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return default;
        }

        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (cachedBytes != null && cachedBytes.Length > 0)
            {
                return JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Failed to read key '{Key}' from distributed cache, attempting memory fallback.", key);
        }

        if (_memoryCache.TryGetValue<T>(key, out var memoryValue))
        {
            return memoryValue;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || value == null)
        {
            return;
        }

        var effectiveExpiration = expiration ?? TimeSpan.FromHours(1);

        // Luôn lưu bản sao vào MemoryCache để dự phòng
        _memoryCache.Set(key, value, effectiveExpiration);

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveExpiration
            };
            await _distributedCache.SetAsync(key, bytes, entryOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Failed to write key '{Key}' to distributed cache. Retained in memory cache.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _memoryCache.Remove(key);

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Failed to remove key '{Key}' from distributed cache.", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var result = await factory(cancellationToken);
        if (result != null)
        {
            await SetAsync(key, result, expiration, cancellationToken);
        }

        return result;
    }
}
