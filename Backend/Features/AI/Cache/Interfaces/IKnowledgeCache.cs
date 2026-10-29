using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

public interface IKnowledgeCache
{
    Task<T?> GetKnowledgeAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetKnowledgeAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<T> GetOrSetKnowledgeAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveKnowledgeAsync(string key, CancellationToken cancellationToken = default);
}
