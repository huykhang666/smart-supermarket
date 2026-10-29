namespace SmartSupermarket.Backend.Infrastructure.Logging;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}
