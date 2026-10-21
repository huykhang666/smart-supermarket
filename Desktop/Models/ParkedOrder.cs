using System;
using System.Collections.Generic;

namespace Desktop.Models;

public class ParkedOrder
{
    public string ParkId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public DateTime ParkedAt { get; set; } = DateTime.Now;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerTier { get; set; } = string.Empty;
    public int CustomerPoints { get; set; }
    public decimal AppliedDiscountAmount { get; set; }
    public string? AppliedVoucherCode { get; set; }
    public int SelectedPaymentMethod { get; set; } = 1;
    public decimal CashPaid { get; set; }
    public List<ParkedOrderItem> Items { get; set; } = new();

    public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
    public decimal FinalAmount => Math.Max(0, TotalAmount - AppliedDiscountAmount);
}

public class ParkedOrderItem
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal SubTotal => Price * Quantity;
}
