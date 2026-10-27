using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Requests;
using SmartSupermarket.Backend.Features.AI.DTOs.Responses;

namespace SmartSupermarket.Backend.Features.AI.Orchestration;

public interface IAgentOrchestrator
{
    Task<AiChatResponseDto> DispatchAsync(
        AiChatRequestDto request,
        CancellationToken cancellationToken = default);
}
