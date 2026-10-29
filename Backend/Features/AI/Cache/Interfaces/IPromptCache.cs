using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

public interface IPromptCache
{
    Task<string?> GetPromptAsync(string key, CancellationToken cancellationToken = default);
    Task SetPromptAsync(string key, string prompt, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<string> GetOrSetPromptAsync(string key, Func<CancellationToken, Task<string>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task InvalidatePromptAsync(string key, CancellationToken cancellationToken = default);
}
