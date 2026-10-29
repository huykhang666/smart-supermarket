using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;

namespace SmartSupermarket.Backend.Features.AI.Cache.Interfaces;

public interface IConversationCache
{
    Task<AgentContextDto?> GetContextAsync(string sessionId, CancellationToken cancellationToken = default);
    Task SetContextAsync(string sessionId, AgentContextDto context, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default);
    Task<List<ChatMessageDto>?> GetHistoryAsync(string sessionId, int limit = 10, CancellationToken cancellationToken = default);
    Task AppendMessageAsync(string sessionId, string role, string content, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default);
    Task ClearAsync(string sessionId, CancellationToken cancellationToken = default);
}
