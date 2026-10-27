using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;

namespace SmartSupermarket.Backend.Features.AI.Services.Context;

public interface IConversationContextService
{
    Task<AgentContextDto> GetOrCreateContextAsync(
        string sessionId,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task SaveContextAsync(
        AgentContextDto context,
        CancellationToken cancellationToken = default);

    Task AppendMessageAsync(
        string sessionId,
        string role,
        string content,
        CancellationToken cancellationToken = default);

    Task<List<ChatMessageDto>> GetHistoryAsync(
        string sessionId,
        int limit = 5,
        CancellationToken cancellationToken = default);

    Task ClearHistoryAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
