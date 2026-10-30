using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;
using SmartSupermarket.Backend.Features.AI.Routing.Models;

namespace SmartSupermarket.Backend.Features.AI.Routing.Interfaces;

public interface IModelRouter
{
    Task<ModelRoutingDecisionDto> RouteRequestAsync(
        string prompt,
        UserIntentDto? intent = null,
        bool hasExactFaqMatch = false,
        CancellationToken cancellationToken = default);
}
