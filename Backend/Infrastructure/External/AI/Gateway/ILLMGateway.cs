using System.Threading;
using System.Threading.Tasks;

namespace SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

public interface ILLMGateway
{
    string ActiveProvider { get; }

    Task<string> GenerateTextAsync(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default);

    Task<string> GenerateJsonAsync(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default);

    Task<T?> GenerateStructuredAsync<T>(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default);
}
