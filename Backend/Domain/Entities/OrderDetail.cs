namespace SmartSupermarket.Backend.Domain.Entities;

public class OrderDetail
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public int ConversionRate { get; set; } = 1;
    public string? UnitName { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}