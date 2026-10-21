using SmartSupermarket.Backend.Features.Orders.DTOs;

namespace SmartSupermarket.Backend.Features.Orders.Services;

public interface IInvoicePdfService
{
    byte[] GenerateInvoicePdf(OrderDto order);
}
