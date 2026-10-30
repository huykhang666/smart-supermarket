using System;
using System.Collections.Generic;

namespace SmartSupermarket.Backend.Features.Reports.DTOs.Responses;

public class RevenueProfitReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalLossAmount { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public PaymentBreakdownDto PaymentBreakdown { get; set; } = new();
    public List<DailyRevenueProfitDto> DailyTrends { get; set; } = new();
}

public class PaymentBreakdownDto
{
    public decimal Cash { get; set; }
    public decimal QrCode { get; set; }
    public decimal CreditCard { get; set; }
    public int CashOrders { get; set; }
    public int QrOrders { get; set; }
    public int CardOrders { get; set; }
}

public class DailyRevenueProfitDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Loss { get; set; }
    public decimal Profit { get; set; }
    public int OrderCount { get; set; }
}

public class BestSellerItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CurrentStock { get; set; }
}

public class InventoryExpiryReportDto
{
    public int TotalExpiringItems { get; set; }
    public int TotalExpiredItems { get; set; }
    public int TotalSlowMovingItems { get; set; }
    public List<ExpiryBatchItemDto> ExpiringBatches { get; set; } = new();
    public List<SlowMovingItemDto> SlowMovingItems { get; set; } = new();
}

public class ExpiryBatchItemDto
{
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class SlowMovingItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int SoldLast30Days { get; set; }
    public decimal Price { get; set; }
    public decimal InventoryValue { get; set; }
}

public class CashierShiftReportDto
{
    public string ReportDate { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalQr { get; set; }
    public decimal TotalCard { get; set; }
    public List<CashierSummaryDto> Cashiers { get; set; } = new();
    public List<ShiftSummaryDto> Shifts { get; set; } = new();
}

public class CashierSummaryDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CashAmount { get; set; }
    public decimal QrAmount { get; set; }
    public decimal CardAmount { get; set; }
}

public class ShiftSummaryDto
{
    public string ShiftName { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CashAmount { get; set; }
    public decimal NonCashAmount { get; set; }
}
