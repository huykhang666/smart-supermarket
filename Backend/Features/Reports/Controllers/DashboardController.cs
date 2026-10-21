using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Reports.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        // Quy đổi thời gian theo múi giờ Việt Nam (UTC+7)
        var vnNow = DateTime.UtcNow.AddHours(7);
        var todayVn = vnNow.Date;
        var todayUtcStart = todayVn.AddHours(-7);
        var tomorrowUtcStart = todayUtcStart.AddDays(1);

        // Doanh thu và đơn hàng hôm nay (chỉ tính đơn hoàn tất, dùng FinalAmount sau chiết khấu)
        var todayOrdersQuery = _context.Orders
            .Where(o => o.OrderDate >= todayUtcStart && o.OrderDate < tomorrowUtcStart
                        && o.Status == Domain.Enums.OrderStatus.Completed);

        var todayRevenue = await todayOrdersQuery
            .SumAsync(o => (decimal?)o.FinalAmount) ?? 0m;

        var todayOrders = await todayOrdersQuery.CountAsync();

        var totalCustomers = await _context.Customers.CountAsync();

        var lowStockCount = await _context.InventoryBatches
            .Where(b => b.Quantity < 10)
            .CountAsync();

        var expiringCount = await _context.InventoryBatches
            .Where(b => b.ExpiryDate <= todayUtcStart.AddDays(7))
            .CountAsync();

        var warningCount = lowStockCount + expiringCount;

        // Biểu đồ doanh thu 7 ngày gần nhất theo múi giờ VN (chỉ tính đơn hoàn tất, dùng FinalAmount)
        var last7DaysVn = Enumerable.Range(0, 7)
            .Select(i => todayVn.AddDays(-6 + i))
            .ToList();

        var chartLabels = last7DaysVn.Select(d => d.ToString("dd/MM")).ToArray();
        var chartValues = new List<double>();

        foreach (var dayVn in last7DaysVn)
        {
            var dayUtcStart = dayVn.AddHours(-7);
            var nextDayUtcStart = dayUtcStart.AddDays(1);
            var dayRevenue = await _context.Orders
                .Where(o => o.OrderDate >= dayUtcStart && o.OrderDate < nextDayUtcStart
                            && o.Status == Domain.Enums.OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.FinalAmount) ?? 0m;

            chartValues.Add((double)dayRevenue);
        }

        // Tạo câu phân tích AI tổng hợp động từ dữ liệu thật
        string aiAnalysis;
        if (todayOrders == 0)
        {
            aiAnalysis = $"“Hệ thống chưa ghi nhận đơn hàng hoàn tất trong ngày {todayVn:dd/MM/yyyy}. Hiện có {totalCustomers} khách hàng thành viên và {warningCount} mặt hàng cần lưu ý kho.”";
        }
        else
        {
            var topCat = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product).ThenInclude(p => p.Category)
                .Where(od => od.Order.OrderDate >= todayUtcStart && od.Order.OrderDate < tomorrowUtcStart
                             && od.Order.Status == Domain.Enums.OrderStatus.Completed
                             && od.Product.Category != null)
                .GroupBy(od => od.Product.Category!.CategoryName)
                .Select(g => new { CategoryName = g.Key, Revenue = g.Sum(x => x.SubTotal) })
                .OrderByDescending(x => x.Revenue)
                .FirstOrDefaultAsync();

            string topCatNotice = topCat != null ? $" Nhóm ngành hàng '{topCat.CategoryName}' đóng góp tỷ trọng doanh thu cao nhất." : "";
            aiAnalysis = $"“Tổng doanh thu thực thu trong ngày đạt {todayRevenue:N0} VNĐ qua {todayOrders} đơn hàng hoàn tất.{topCatNotice}”";
        }

        return Ok(new
        {
            status = 200,
            data = new
            {
                todayRevenue = todayRevenue.ToString("N0") + " đ",
                todayOrders = todayOrders.ToString("N0") + " đơn",
                totalCustomers = totalCustomers.ToString("N0") + " thành viên",
                warningCount = warningCount.ToString("N0") + " mặt hàng",
                chartLabels,
                chartValues,
                aiAnalysis
            }
        });
    }
}
