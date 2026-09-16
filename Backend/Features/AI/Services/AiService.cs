using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Features.AI.DTOs;
using SmartSupermarket.Backend.Infrastructure.External.AI;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.AI.Services;

public class AiService : IAiService
{
    private readonly GeminiService _gemini;
    private readonly AppDbContext _dbContext;

    public AiService(GeminiService gemini, AppDbContext dbContext)
    {
        _gemini = gemini;
        _dbContext = dbContext;
    }

    public async Task<AiResponse> GenerateImportForecastAsync(AiImportForecastRequest request, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-request.DaysBack);

        var salesData = await _dbContext.OrderDetails
            .Include(od => od.Order)
            .Where(od => od.Order.OrderDate >= since && od.Order.Status == Domain.Enums.OrderStatus.Completed)
            .GroupBy(od => od.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQty = g.Sum(x => x.Quantity),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalQty)
            .Take(20)
            .ToListAsync(cancellationToken);

        var productIds = salesData.Select(s => s.ProductId).ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId, p => p.ProductName, cancellationToken);

        var inventories = await _dbContext.Inventories
            .Where(i => productIds.Contains(i.ProductId))
            .ToDictionaryAsync(i => i.ProductId, i => i.QuantityOnHand, cancellationToken);

        var lines = salesData.Select(s =>
        {
            string name = products.TryGetValue(s.ProductId, out var n) ? n : $"SP-{s.ProductId}";
            int stock = inventories.TryGetValue(s.ProductId, out var qty) ? qty : 0;
            return $"- {name}: đã bán {s.TotalQty} sản phẩm trong {request.DaysBack} ngày, tồn kho hiện tại: {stock}";
        });

        string dataBlock = string.Join("\n", lines);
        string prompt = $"""
            Bạn là chuyên gia quản lý chuỗi siêu thị bán lẻ tại Việt Nam.
            Dưới đây là dữ liệu bán hàng {request.DaysBack} ngày gần nhất của siêu thị Smart SuperMarket:

            {dataBlock}

            Dựa trên dữ liệu trên, hãy:
            1. Đánh giá xu hướng bán hàng của từng sản phẩm
            2. Dự báo số lượng cần nhập kho cho 7 ngày tới (cụ thể từng sản phẩm, có lý giải)
            3. Cảnh báo những sản phẩm có nguy cơ thiếu hàng
            4. Đề xuất thứ tự ưu tiên nhập hàng

            Trả lời bằng tiếng Việt, có đánh số, rõ ràng và có thể hành động ngay.
            """;

        string content = await _gemini.GenerateAsync(prompt, cancellationToken);
        return new AiResponse { Content = content, Model = "gemini-1.5-flash" };
    }

    public async Task<AiResponse> GenerateRevenueReportAsync(AiRevenueReportRequest request, CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Orders
            .Where(o => o.OrderDate >= request.StartDate && o.OrderDate <= request.EndDate
                        && o.Status == Domain.Enums.OrderStatus.Completed)
            .ToListAsync(cancellationToken);

        decimal totalRevenue = orders.Sum(o => o.FinalAmount);
        decimal totalDiscount = orders.Sum(o => o.DiscountAmount);
        int orderCount = orders.Count;

        var cashOrders = orders.Count(o => o.PaymentMethod == Domain.Enums.PaymentMethod.Cash);
        var qrOrders = orders.Count(o => o.PaymentMethod == Domain.Enums.PaymentMethod.QRCode);
        var cardOrders = orders.Count(o => o.PaymentMethod == Domain.Enums.PaymentMethod.CreditCard);

        var topProducts = await _dbContext.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.Product)
            .Where(od => od.Order.OrderDate >= request.StartDate && od.Order.OrderDate <= request.EndDate
                         && od.Order.Status == Domain.Enums.OrderStatus.Completed)
            .GroupBy(od => new { od.ProductId, od.Product.ProductName })
            .Select(g => new { g.Key.ProductName, Revenue = g.Sum(x => x.SubTotal), Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync(cancellationToken);

        string topProductsText = string.Join("\n", topProducts.Select((p, i) =>
            $"  {i + 1}. {p.ProductName}: {p.Revenue:N0} VNĐ ({p.Qty} sản phẩm)"));

        string prompt = $"""
            Bạn là nhà phân tích kinh doanh siêu thị tại Việt Nam.
            Dưới đây là số liệu doanh thu của Smart SuperMarket từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy}:

            - Tổng doanh thu: {totalRevenue:N0} VNĐ
            - Tổng chiết khấu / khuyến mãi đã áp dụng: {totalDiscount:N0} VNĐ
            - Số đơn hàng: {orderCount} đơn
            - Doanh thu trung bình / đơn: {(orderCount > 0 ? totalRevenue / orderCount : 0):N0} VNĐ
            - Thanh toán: Tiền mặt {cashOrders} đơn | VietQR {qrOrders} đơn | Thẻ POS {cardOrders} đơn
            - Top 5 sản phẩm bán chạy nhất:
            {topProductsText}

            Hãy viết một báo cáo doanh thu chuyên nghiệp bằng tiếng Việt gồm:
            1. Tóm tắt tình hình kinh doanh (1 đoạn)
            2. Phân tích điểm mạnh và điểm cần cải thiện
            3. Nhận xét về xu hướng thanh toán
            4. Đề xuất chiến lược kinh doanh cho tháng tới (ít nhất 3 khuyến nghị cụ thể)
            """;

        string content = await _gemini.GenerateAsync(prompt, cancellationToken);
        return new AiResponse { Content = content, Model = "gemini-1.5-flash" };
    }

    public async Task<AiResponse> GenerateProductAnalysisAsync(AiProductAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-request.DaysBack);

        var topProducts = await _dbContext.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.Product)
            .Where(od => od.Order.OrderDate >= since && od.Order.Status == Domain.Enums.OrderStatus.Completed)
            .GroupBy(od => new { od.ProductId, od.Product.ProductName, od.Product.Price })
            .Select(g => new
            {
                g.Key.ProductName,
                g.Key.Price,
                TotalQty = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.SubTotal),
                OrderFrequency = g.Count()
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(request.TopN)
            .ToListAsync(cancellationToken);

        string productLines = string.Join("\n", topProducts.Select((p, i) =>
            $"  {i + 1}. {p.ProductName} (giá {p.Price:N0}đ): bán {p.TotalQty} sản phẩm, doanh thu {p.TotalRevenue:N0}đ, xuất hiện trong {p.OrderFrequency} đơn"));

        string prompt = $"""
            Bạn là chuyên gia phân tích hàng hóa siêu thị tại Việt Nam.
            Đây là Top {request.TopN} sản phẩm bán chạy nhất trong {request.DaysBack} ngày qua của Smart SuperMarket:

            {productLines}

            Hãy phân tích chuyên sâu và cung cấp:
            1. Nhận định về danh mục sản phẩm nào đang dẫn đầu và lý do
            2. Tính toán biên lợi nhuận ước tính (gross margin estimate)
            3. Sản phẩm nào có tiềm năng tăng trưởng cao – và tại sao
            4. Chiến lược bày trí kệ hàng (planogram) để tối ưu doanh thu
            5. Gợi ý sản phẩm nên bổ sung vào danh mục hoặc loại bỏ

            Viết bằng tiếng Việt, súc tích, có số liệu cụ thể, dùng cho ban quản lý siêu thị.
            """;

        string content = await _gemini.GenerateAsync(prompt, cancellationToken);
        return new AiResponse { Content = content, Model = "gemini-1.5-flash" };
    }
}
