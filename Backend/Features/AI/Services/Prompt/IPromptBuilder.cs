using System.Threading;
using System.Threading.Tasks;

namespace SmartSupermarket.Backend.Features.AI.Services.Prompt;

public interface IPromptBuilder
{
    Task<string> BuildSystemPromptAsync(string role = "shopping", CancellationToken cancellationToken = default);

    Task<string> BuildUserPromptWithContextAsync(
        string rawUserPrompt,
        string? historyContext = null,
        string? cartContext = null,
        CancellationToken cancellationToken = default);

    Task<string> GetLayerContentAsync(string layerKey, CancellationToken cancellationToken = default);
}
