using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Orders.DTOs;

public class OrderDto
{
    public int OrderId { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int BranchId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public int? VoucherId { get; set; }
    public decimal FinalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public List<OrderDetailDto> OrderDetails { get; set; } = new();
}

public class OrderDetailDto
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class CreateOrderRequest
{
    public int EmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public int BranchId { get; set; } = 1;
    public int? VoucherId { get; set; }
    /// <summary>Mã khuyến mãi nhập từ thu ngân POS (nếu có)</summary>
    public string? PromotionCode { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public List<CreateOrderDetailRequest> Items { get; set; } = new();
}

public class CreateOrderDetailRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderPagedResult
{
    public IEnumerable<OrderDto> Items { get; set; } = new List<OrderDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
