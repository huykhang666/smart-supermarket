using System.Threading;
using System.Threading.Tasks;
using SmartSupermarket.Backend.Features.AI.DTOs.Common;

namespace SmartSupermarket.Backend.Features.AI.Services.Intent;

public interface IIntentAnalyzer
{
    Task<UserIntentDto> AnalyzeIntentAsync(string prompt, CancellationToken cancellationToken = default);
}
