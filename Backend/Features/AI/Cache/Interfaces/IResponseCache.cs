using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

public interface IResponseCache
{
    Task<string?> GetResponseAsync(string prompt, string? systemInstruction = null, bool requireJson = false, CancellationToken cancellationToken = default);
    Task SetResponseAsync(string prompt, string response, string? systemInstruction = null, bool requireJson = false, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task ClearResponseCacheAsync(CancellationToken cancellationToken = default);
}
