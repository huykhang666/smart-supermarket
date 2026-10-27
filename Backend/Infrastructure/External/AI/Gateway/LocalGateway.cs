using System.Threading;
using System.Threading.Tasks;

namespace SmartSupermarket.Backend.Infrastructure.External.AI.Gateway;

public class LocalGateway : LocalFallbackGateway
{
    public override Task<string> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        bool requireJson = false,
        CancellationToken cancellationToken = default)
    {
        return base.GenerateAsync(prompt, systemInstruction, requireJson, cancellationToken);
    }
}
