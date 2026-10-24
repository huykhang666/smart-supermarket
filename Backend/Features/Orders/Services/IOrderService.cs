using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Orders.DTOs;

namespace SmartSupermarket.Backend.Features.Orders.Services;

public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderPagedResult> GetPagedAsync(
        int? branchId,
        int? employeeId,
        int? customerId,
        OrderStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<bool> CancelOrderAsync(int orderId, CancelOrderRequest? request = null, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateInvoicePdfAsync(int orderId, CancellationToken cancellationToken = default);
}