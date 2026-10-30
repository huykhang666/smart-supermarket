using System.Collections.Generic;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Orders.DTOs.Requests;

public class CreatePaymentRequest
{
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal AmountPaid { get; set; }
}

public class CreateOrderRequest
{
    public int EmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public int BranchId { get; set; } = 1;
    public int? ShiftSessionId { get; set; }
    public int? VoucherId { get; set; }
    public string? PromotionCode { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public List<CreatePaymentRequest> Payments { get; set; } = new();
    public List<CreateOrderDetailRequest> Items { get; set; } = new();
}

public class CancelOrderRequest
{
    public string? ManagerPin { get; set; }
    public string? ManagerUsername { get; set; }
    public string? ManagerPassword { get; set; }
    public string CancelReason { get; set; } = "Khách hàng đổi ý";
    public string? Note { get; set; }
    public int? CancelledByUserId { get; set; }
}

public class CreateOrderDetailRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int ConversionRate { get; set; } = 1;
    public string? UnitName { get; set; }
}


public class RefundOrderItemRequest
{
    public int ProductId { get; set; }
    public int QuantityToRefund { get; set; }
    public string? Reason { get; set; }
}

public class RefundOrderRequest
{
    public int OrderId { get; set; }
    public int EmployeeId { get; set; } = 1;
    public string Reason { get; set; } = "Khách đổi trả hàng tại quầy";
    public List<RefundOrderItemRequest> Items { get; set; } = new();
}
