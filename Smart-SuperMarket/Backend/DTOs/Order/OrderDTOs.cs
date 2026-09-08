namespace SmartSupermarket.Backend.DTOs.Order;

public class CreateOrderRequest
{
    public int? CustomerId { get; set; }
    public int BranchId { get; set; }
    public int? VoucherId { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
    public int PaymentMethod { get; set; } // 1=Cash, 2=QR, 3=Card
}

public class OrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderResponse
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}
