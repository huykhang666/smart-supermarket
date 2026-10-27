using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Agents.Common;

public interface IAgent
{
    string AgentName { get; }

    string Role { get; }

    Task<AgentResultDto> ExecuteAsync(
        AgentContextDto context,
        CancellationToken cancellationToken = default);
}
