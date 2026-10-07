using SmartSupermarket.Backend.Features.Payments.DTOs;

namespace SmartSupermarket.Backend.Features.Payments.Services;

public interface IPaymentService
{
    Task<PaymentTransactionResponse> CreateTransactionAsync(CreatePaymentTransactionRequest request);
    Task<PaymentTransactionResponse?> GetTransactionByIdAsync(int id);
    Task<PaymentStatusResponse?> GetTransactionStatusAsync(int id);
    Task<ZaloPayCallbackResponse> ProcessZaloPayCallbackAsync(ZaloPayCallbackPayload payload);
    Task<bool> CancelTransactionAsync(int id);
    Task<SyncGatewayStatusResponse> SyncGatewayStatusAsync(int id);
    Task<(IEnumerable<PaymentTransactionResponse> items, int totalItems)> ListTransactionsAsync(PaymentTransactionFilterParams filterParams);
}
