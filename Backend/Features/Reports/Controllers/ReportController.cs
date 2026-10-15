using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Reports.DTOs;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Reports.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 1. Báo cáo Doanh thu, Giá vốn và Lãi/Lỗ gộp theo khoảng ngày
    /// </summary>
    [HttpGet("revenue-profit")]
    public async Task<ActionResult<ApiResult<RevenueProfitReportDto>>> GetRevenueProfit(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken = default)
    {
        // Mặc định 7 ngày gần nhất theo giờ Việt Nam (UTC+7)
        var vnNow = DateTime.UtcNow.AddHours(7);
        var endVn = (endDate ?? vnNow).Date.AddDays(1).AddTicks(-1); // Cuối ngày kết thúc
        var startVn = (startDate ?? vnNow.AddDays(-6)).Date;        // Đầu ngày bắt đầu

        // Quy đổi sang UTC để query Database
        var startUtc = startVn.AddHours(-7);
        var endUtc = endVn.AddHours(-7);

        var query = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.Payments)
            .Where(o => o.OrderDate >= startUtc && o.OrderDate <= endUtc
                        && o.Status == OrderStatus.Completed);

        if (branchId.HasValue)
        {
            query = query.Where(o => o.BranchId == branchId.Value);
        }

        var orders = await query.ToListAsync(cancellationToken);

        int totalOrders = orders.Count;
        decimal totalRevenue = orders.Sum(o => o.FinalAmount);
        decimal totalDiscount = orders.Sum(o => o.DiscountAmount);

        // Tính giá vốn: lấy Product.CostPrice, nếu null hoặc 0 quy ước 70% giá bán
        decimal totalCost = 0;
        foreach (var order in orders)
        {
            foreach (var detail in order.OrderDetails)
            {
                decimal cost = (detail.Product != null && detail.Product.CostPrice.HasValue && detail.Product.CostPrice.Value > 0)
                    ? detail.Product.CostPrice.Value
                    : (detail.Product != null ? detail.Product.Price * 0.7m : detail.UnitPrice * 0.7m);

                totalCost += cost * detail.Quantity;
            }
        }

        decimal grossProfit = totalRevenue - totalCost;
        decimal profitMargin = totalRevenue > 0 ? Math.Round((grossProfit / totalRevenue) * 100m, 2) : 0m;
        decimal avgOrder = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 0) : 0m;

        // Phân rã phương thức thanh toán
        var paymentBreakdown = new PaymentBreakdownDto();
        foreach (var order in orders)
        {
            if (order.Payments != null && order.Payments.Any())
            {
                foreach (var p in order.Payments)
                {
                    if (p.PaymentMethod == PaymentMethod.Cash)
                    {
                        paymentBreakdown.Cash += p.AmountPaid > order.FinalAmount ? order.FinalAmount : p.AmountPaid;
                        paymentBreakdown.CashOrders++;
                    }
                    else if (p.PaymentMethod == PaymentMethod.QRCode)
                    {
                        paymentBreakdown.QrCode += p.AmountPaid;
                        paymentBreakdown.QrOrders++;
                    }
                    else if (p.PaymentMethod == PaymentMethod.CreditCard)
                    {
                        paymentBreakdown.CreditCard += p.AmountPaid;
                        paymentBreakdown.CardOrders++;
                    }
                }
            }
            else
            {
                paymentBreakdown.Cash += order.FinalAmount;
                paymentBreakdown.CashOrders++;
            }
        }

        // Xu hướng theo từng ngày (múi giờ VN)
        var dailyTrends = new List<DailyRevenueProfitDto>();
        int totalDays = (int)(endVn.Date - startVn.Date).TotalDays + 1;
        for (int i = 0; i < totalDays; i++)
        {
            var curDayVn = startVn.AddDays(i);
            var curStartUtc = curDayVn.AddHours(-7);
            var curEndUtc = curDayVn.AddDays(1).AddHours(-7);

            var dayOrders = orders.Where(o => o.OrderDate >= curStartUtc && o.OrderDate < curEndUtc).ToList();
            decimal dayRev = dayOrders.Sum(o => o.FinalAmount);
            decimal dayCost = 0;
            foreach (var o in dayOrders)
            {
                foreach (var d in o.OrderDetails)
                {
                    decimal c = (d.Product != null && d.Product.CostPrice.HasValue && d.Product.CostPrice.Value > 0)
                        ? d.Product.CostPrice.Value
                        : (d.Product != null ? d.Product.Price * 0.7m : d.UnitPrice * 0.7m);
                    dayCost += c * d.Quantity;
                }
            }

            dailyTrends.Add(new DailyRevenueProfitDto
            {
                Date = curDayVn.ToString("dd/MM/yyyy"),
                Revenue = dayRev,
                Cost = dayCost,
                Profit = dayRev - dayCost,
                OrderCount = dayOrders.Count
            });
        }

        var report = new RevenueProfitReportDto
        {
            StartDate = startVn,
            EndDate = endVn,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            GrossProfit = grossProfit,
            ProfitMargin = profitMargin,
            TotalDiscount = totalDiscount,
            AverageOrderValue = avgOrder,
            PaymentBreakdown = paymentBreakdown,
            DailyTrends = dailyTrends
        };

        return Ok(ApiResult<RevenueProfitReportDto>.Success(report, "Lấy báo cáo doanh thu & lãi lỗ thành công"));
    }

    /// <summary>
    /// 2. Báo cáo Top sản phẩm bán chạy nhất
    /// </summary>
    [HttpGet("best-sellers")]
    public async Task<ActionResult<ApiResult<List<BestSellerItemDto>>>> GetBestSellers(
        [FromQuery] int top = 10,
        [FromQuery] int days = 7,
        [FromQuery] int? branchId = 1,
        CancellationToken cancellationToken = default)
    {
        var vnNow = DateTime.UtcNow.AddHours(7);
        var startUtc = vnNow.AddDays(-days).Date.AddHours(-7);

        var query = _context.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.Product)
                .ThenInclude(p => p.Category)
            .Where(od => od.Order.OrderDate >= startUtc && od.Order.Status == OrderStatus.Completed);

        if (branchId.HasValue)
        {
            query = query.Where(od => od.Order.BranchId == branchId.Value);
        }

        var grouped = await query
            .GroupBy(od => new
            {
                od.ProductId,
                od.Product.ProductName,
                od.Product.Barcode,
                CategoryName = od.Product.Category != null ? od.Product.Category.CategoryName : "Khác"
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.Barcode,
                g.Key.CategoryName,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.SubTotal)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(top)
            .ToListAsync(cancellationToken);

        // Lấy tồn kho hiện tại cho các sản phẩm trong top
        var productIds = grouped.Select(x => x.ProductId).ToList();
        var inventories = await _context.Inventories
            .Where(i => productIds.Contains(i.ProductId) && (!branchId.HasValue || i.BranchId == branchId.Value))
            .ToDictionaryAsync(i => i.ProductId, i => i.QuantityOnHand, cancellationToken);

        var result = grouped.Select(x => new BestSellerItemDto
        {
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            Barcode = x.Barcode,
            CategoryName = x.CategoryName,
            QuantitySold = x.QuantitySold,
            TotalRevenue = x.TotalRevenue,
            CurrentStock = inventories.TryGetValue(x.ProductId, out var stock) ? stock : 0
        }).ToList();

        return Ok(ApiResult<List<BestSellerItemDto>>.Success(result, $"Lấy top {result.Count} sản phẩm bán chạy thành công"));
    }

    /// <summary>
    /// 3. Báo cáo Tồn kho Cận Hạn Sử Dụng & Hàng Chậm Luân Chuyển
    /// </summary>
    [HttpGet("inventory-expiry")]
    public async Task<ActionResult<ApiResult<InventoryExpiryReportDto>>> GetInventoryExpiry(
        [FromQuery] int daysThreshold = 5,
        [FromQuery] int? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var vnNow = DateTime.UtcNow.AddHours(7).Date;
        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);

        var batchQuery = _context.InventoryBatches
            .Include(b => b.Product)
                .ThenInclude(p => p.Category)
            .Where(b => b.Quantity > 0 && b.ExpiryDate <= thresholdDate);

        var batches = await batchQuery
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(cancellationToken);

        var expiringBatches = batches.Select(b =>
        {
            var expVn = b.ExpiryDate.AddHours(7).Date;
            int daysRemaining = (int)(expVn - vnNow).TotalDays;
            string statusStr;
            if (daysRemaining < 0)
                statusStr = "Đã hết hạn";
            else if (daysRemaining <= 5)
                statusStr = "Cận hạn nguy cấp (≤5 ngày)";
            else
                statusStr = $"Cảnh báo ({daysRemaining} ngày)";

            return new ExpiryBatchItemDto
            {
                BatchId = b.BatchId,
                BatchCode = b.BatchCode,
                ProductId = b.ProductId,
                ProductName = b.Product?.ProductName ?? "Sản phẩm #" + b.ProductId,
                Barcode = b.Product?.Barcode ?? "",
                CategoryName = b.Product?.Category?.CategoryName ?? "Chung",
                Quantity = b.Quantity,
                ManufacturingDate = b.ManufacturingDate,
                ExpiryDate = b.ExpiryDate,
                DaysRemaining = daysRemaining,
                StorageLocation = b.StorageLocation,
                Status = statusStr
            };
        }).ToList();

        // Tìm hàng tồn kho chậm luân chuyển (tồn > 10 mà 30 ngày qua bán ít hơn 3)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentSales = await _context.OrderDetails
            .Where(od => od.Order.OrderDate >= thirtyDaysAgo && od.Order.Status == OrderStatus.Completed)
            .GroupBy(od => od.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Sold, cancellationToken);

        var invQuery = _context.Inventories
            .Include(i => i.Product)
                .ThenInclude(p => p.Category)
            .Where(i => i.QuantityOnHand >= 10);

        if (branchId.HasValue)
        {
            invQuery = invQuery.Where(i => i.BranchId == branchId.Value);
        }

        var candidateInventories = await invQuery.ToListAsync(cancellationToken);

        var slowMoving = candidateInventories
            .Where(i => i.Product != null && (!recentSales.TryGetValue(i.ProductId, out var sold) || sold <= 2))
            .Select(i => new SlowMovingItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.ProductName ?? "",
                Barcode = i.Product?.Barcode ?? "",
                CategoryName = i.Product?.Category?.CategoryName ?? "",
                QuantityOnHand = i.QuantityOnHand,
                SoldLast30Days = recentSales.TryGetValue(i.ProductId, out var s) ? s : 0,
                Price = i.Product?.Price ?? 0,
                InventoryValue = i.QuantityOnHand * (i.Product?.Price ?? 0)
            })
            .OrderByDescending(x => x.InventoryValue)
            .Take(30)
            .ToList();

        var report = new InventoryExpiryReportDto
        {
            TotalExpiredItems = expiringBatches.Where(b => b.DaysRemaining < 0).Sum(b => b.Quantity),
            TotalExpiringItems = expiringBatches.Where(b => b.DaysRemaining >= 0).Sum(b => b.Quantity),
            TotalSlowMovingItems = slowMoving.Count,
            ExpiringBatches = expiringBatches,
            SlowMovingItems = slowMoving
        };

        return Ok(ApiResult<InventoryExpiryReportDto>.Success(report, "Lấy báo cáo tồn kho & cận hạn thành công"));
    }

    /// <summary>
    /// 4. Báo cáo Ca trực & Thu ngân (Cashier & Shift Reconciliation)
    /// </summary>
    [HttpGet("cashier-shift")]
    public async Task<ActionResult<ApiResult<CashierShiftReportDto>>> GetCashierShiftReport(
        [FromQuery] DateTime? date,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken = default)
    {
        var targetVnDate = (date ?? DateTime.UtcNow.AddHours(7)).Date;
        var startUtc = targetVnDate.AddHours(-7);
        var endUtc = targetVnDate.AddDays(1).AddHours(-7);

        var query = _context.Orders
            .Include(o => o.Employee)
            .Include(o => o.Payments)
            .Where(o => o.OrderDate >= startUtc && o.OrderDate < endUtc
                        && o.Status == OrderStatus.Completed);

        if (branchId.HasValue)
        {
            query = query.Where(o => o.BranchId == branchId.Value);
        }

        var orders = await query.ToListAsync(cancellationToken);

        // Group theo thu ngân (EmployeeId)
        var cashierGroups = orders.GroupBy(o => o.EmployeeId).ToList();
        var cashierSummaries = new List<CashierSummaryDto>();

        foreach (var g in cashierGroups)
        {
            var emp = g.FirstOrDefault()?.Employee;
            decimal cash = 0;
            decimal qr = 0;
            decimal card = 0;

            foreach (var o in g)
            {
                if (o.Payments != null && o.Payments.Any())
                {
                    foreach (var p in o.Payments)
                    {
                        if (p.PaymentMethod == PaymentMethod.Cash)
                            cash += p.AmountPaid > o.FinalAmount ? o.FinalAmount : p.AmountPaid;
                        else if (p.PaymentMethod == PaymentMethod.QRCode)
                            qr += p.AmountPaid;
                        else if (p.PaymentMethod == PaymentMethod.CreditCard)
                            card += p.AmountPaid;
                    }
                }
                else
                {
                    cash += o.FinalAmount;
                }
            }

            cashierSummaries.Add(new CashierSummaryDto
            {
                UserId = g.Key,
                Username = emp?.Username ?? $"staff_{g.Key}",
                FullName = emp?.FullName ?? $"Thu ngân #{g.Key}",
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.FinalAmount),
                CashAmount = cash,
                QrAmount = qr,
                CardAmount = card
            });
        }

        // Group theo Ca làm việc (Quy đổi OrderDate sang giờ VN)
        // Ca Sáng: 06:00 đến 14:00
        // Ca Chiều-Tối: 14:00 đến 22:00
        // Ca Khác: ngoài khung trên
        var morningOrders = new List<SmartSupermarket.Backend.Domain.Entities.Order>();
        var eveningOrders = new List<SmartSupermarket.Backend.Domain.Entities.Order>();
        var otherOrders = new List<SmartSupermarket.Backend.Domain.Entities.Order>();

        var morningStart = TimeSpan.FromHours(6);
        var morningEnd = TimeSpan.FromHours(14);
        var eveningEnd = TimeSpan.FromHours(22);

        foreach (var o in orders)
        {
            var vnTime = o.OrderDate.AddHours(7).TimeOfDay;
            if (vnTime >= morningStart && vnTime < morningEnd)
                morningOrders.Add(o);
            else if (vnTime >= morningEnd && vnTime < eveningEnd)
                eveningOrders.Add(o);
            else
                otherOrders.Add(o);
        }

        ShiftSummaryDto BuildShiftSummary(string name, string timeRange, List<SmartSupermarket.Backend.Domain.Entities.Order> shiftOrders)
        {
            decimal cash = 0;
            decimal nonCash = 0;
            foreach (var o in shiftOrders)
            {
                if (o.Payments != null && o.Payments.Any())
                {
                    foreach (var p in o.Payments)
                    {
                        if (p.PaymentMethod == PaymentMethod.Cash)
                            cash += p.AmountPaid > o.FinalAmount ? o.FinalAmount : p.AmountPaid;
                        else
                            nonCash += p.AmountPaid;
                    }
                }
                else
                {
                    cash += o.FinalAmount;
                }
            }

            return new ShiftSummaryDto
            {
                ShiftName = name,
                TimeRange = timeRange,
                TotalOrders = shiftOrders.Count,
                TotalRevenue = shiftOrders.Sum(o => o.FinalAmount),
                CashAmount = cash,
                NonCashAmount = nonCash
            };
        }

        var shifts = new List<ShiftSummaryDto>
        {
            BuildShiftSummary("Ca Sáng", "06:00 - 14:00", morningOrders),
            BuildShiftSummary("Ca Chiều - Tối", "14:00 - 22:00", eveningOrders)
        };

        if (otherOrders.Any())
        {
            shifts.Add(BuildShiftSummary("Ca Khác", "22:00 - 06:00", otherOrders));
        }

        decimal totalRev = orders.Sum(o => o.FinalAmount);
        decimal totalCash = cashierSummaries.Sum(c => c.CashAmount);
        decimal totalQr = cashierSummaries.Sum(c => c.QrAmount);
        decimal totalCard = cashierSummaries.Sum(c => c.CardAmount);

        var report = new CashierShiftReportDto
        {
            ReportDate = targetVnDate.ToString("dd/MM/yyyy"),
            TotalOrders = orders.Count,
            TotalRevenue = totalRev,
            TotalCash = totalCash,
            TotalQr = totalQr,
            TotalCard = totalCard,
            Cashiers = cashierSummaries,
            Shifts = shifts
        };

        return Ok(ApiResult<CashierShiftReportDto>.Success(report, "Lấy báo cáo ca trực & đối soát thu ngân thành công"));
    }
}
