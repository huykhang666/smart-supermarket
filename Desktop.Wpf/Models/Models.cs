using System;
using System.Collections.Generic;

namespace SmartSupermarket.POS.Models;

public class ProductItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Unit { get; set; } = "Cái";
    public int Stock { get; set; } = 10;
    public string Status { get; set; } = "Đang bán";
    public bool IsNearExpiry { get; set; }
    public string FormattedPrice => $"{Price:N0} đ";
    public string FormattedStock => $"Tồn: {Stock} {Unit}";
}

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; } = 0;
    public decimal TotalAmount => (UnitPrice * Quantity) * (1 - DiscountPercent / 100m);
    public string FormattedTotal => $"{TotalAmount:N0} đ";
    public string FormattedUnitPrice => $"{UnitPrice:N0} đ";
}

public class OrderItem
{
    public int OrderId { get; set; }
    public string OrderCode => $"ORD-{OrderId:D5}";
    public string CustomerName { get; set; } = "Khách Lẻ";
    public string EmployeeName { get; set; } = "Thu Ngân";
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Hoàn thành";
    public string FormattedFinalAmount => $"{FinalAmount:N0} đ";
    public string FormattedDate => OrderDate.ToString("dd/MM/yyyy HH:mm");
}

public class CustomerItem
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; }
    public string Tier { get; set; } = "Thành viên mới";
}
