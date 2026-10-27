using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Services.Planning;

public interface IPlanner
{
    Task<ExecutionPlanDto> CreatePlanAsync(
        UserIntentDto intent,
        AgentContextDto context,
        CancellationToken cancellationToken = default);
}
