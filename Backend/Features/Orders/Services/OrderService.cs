using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Orders.DTOs;
using SmartSupermarket.Backend.Features.Orders.Repositories;
using SmartSupermarket.Backend.Features.Inventory.Repositories;
using SmartSupermarket.Backend.Features.Promotions.Repositories;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly AppDbContext _dbContext;

    public OrderService(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IPromotionRepository promotionRepository,
        AppDbContext dbContext)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _promotionRepository = promotionRepository;
        _dbContext = dbContext;
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null) return null;

        return await MapToDtoAsync(order, cancellationToken);
    }

    public async Task<OrderPagedResult> GetPagedAsync(
        int? employeeId,
        int? customerId,
        OrderStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _orderRepository.GetPagedAsync(
            employeeId, customerId, status, startDate, endDate, page, pageSize, cancellationToken);

        var dtos = new List<OrderDto>();
        foreach (var order in items)
        {
            dtos.Add(await MapToDtoAsync(order, cancellationToken));
        }

        return new OrderPagedResult
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            throw new ArgumentException("Đơn hàng phải chứa ít nhất một sản phẩm.");
        }

        // --- 1. Validate products exist ---
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out _))
            {
                throw new KeyNotFoundException($"Không tìm thấy sản phẩm có ID = {item.ProductId}");
            }
        }

        // --- 2. Validate inventory & deduct stock ---
        var inventoryItems = new List<(Domain.Entities.Inventory Inv, int Qty)>();
        foreach (var item in request.Items)
        {
            var inv = await _inventoryRepository.GetByProductAndBranchAsync(item.ProductId, request.BranchId);
            if (inv == null || inv.QuantityOnHand < item.Quantity)
            {
                var productName = products[item.ProductId].ProductName;
                int available = inv?.QuantityOnHand ?? 0;
                throw new InvalidOperationException(
                    $"Sản phẩm '{productName}' không đủ tồn kho. Cần {item.Quantity}, còn {available}.");
            }
            inventoryItems.Add((inv, item.Quantity));
        }

        // --- 3. Compute line items & total ---
        decimal totalAmount = 0;
        var orderDetails = new List<OrderDetail>();

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            decimal unitPrice = item.UnitPrice > 0 ? item.UnitPrice : product.Price;
            decimal subTotal = unitPrice * item.Quantity;
            totalAmount += subTotal;

            orderDetails.Add(new OrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                SubTotal = subTotal
            });
        }

        // --- 4. Apply voucher/promotion if provided ---
        decimal discountAmount = 0;
        int? resolvedVoucherId = request.VoucherId;

        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var promotion = await _promotionRepository.GetByCodeAsync(request.PromotionCode, cancellationToken);
            if (promotion != null && promotion.IsActive)
            {
                DateTime now = DateTime.UtcNow;
                if (now >= promotion.StartDate && now <= promotion.EndDate
                    && totalAmount >= promotion.MinimumOrderAmount)
                {
                    if (promotion.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
                    {
                        discountAmount = totalAmount * (promotion.DiscountValue / 100m);
                        if (promotion.MaximumDiscountAmount.HasValue && discountAmount > promotion.MaximumDiscountAmount.Value)
                        {
                            discountAmount = promotion.MaximumDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        discountAmount = promotion.DiscountValue;
                    }

                    if (discountAmount > totalAmount)
                    {
                        discountAmount = totalAmount;
                    }

                    // Map promotionId to VoucherId if not already set
                    resolvedVoucherId ??= promotion.PromotionId;
                }
            }
        }

        decimal finalAmount = totalAmount - discountAmount;

        // --- 5. Create order ---
        var order = new Order
        {
            EmployeeId = request.EmployeeId,
            CustomerId = request.CustomerId,
            BranchId = request.BranchId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount,
            VoucherId = resolvedVoucherId,
            FinalAmount = finalAmount,
            PaymentMethod = request.PaymentMethod,
            Status = OrderStatus.Completed,
            OrderDetails = orderDetails
        };

        await _orderRepository.AddAsync(order, cancellationToken);

        // --- 6. Deduct inventory & write stock history ---
        foreach (var (inv, qty) in inventoryItems)
        {
            int qtyBefore = inv.QuantityOnHand;
            inv.QuantityOnHand -= qty;
            inv.LastUpdated = DateTime.UtcNow;
            await _inventoryRepository.UpsertAsync(inv);

            await _inventoryRepository.AddStockHistoryAsync(new StockHistory
            {
                ProductId = inv.ProductId,
                BranchId = inv.BranchId,
                ChangeType = StockChangeType.Sale,
                QuantityChange = -qty,
                QuantityBefore = qtyBefore,
                QuantityAfter = inv.QuantityOnHand,
                Note = $"Bán hàng - Đơn #{order.OrderId}",
                CreatedByUserId = request.EmployeeId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _inventoryRepository.SaveChangesAsync();
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(order, cancellationToken);
    }

    public async Task<bool> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng có ID = {orderId}");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Đơn hàng đã được hủy trước đó.");
        }

        order.Status = OrderStatus.Cancelled;
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<OrderDto> MapToDtoAsync(Order order, CancellationToken cancellationToken)
    {
        var productIds = order.OrderDetails.Select(od => od.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId, cancellationToken);

        string? employeeName = null;
        if (order.EmployeeId > 0)
        {
            var user = await _dbContext.Users.FindAsync(new object[] { order.EmployeeId }, cancellationToken);
            employeeName = user?.FullName;
        }

        string? customerName = null;
        if (order.CustomerId.HasValue)
        {
            var customer = await _dbContext.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId.Value, cancellationToken);
            customerName = customer?.User?.FullName;
        }

        var detailDtos = order.OrderDetails.Select(od =>
        {
            products.TryGetValue(od.ProductId, out var p);
            return new OrderDetailDto
            {
                OrderDetailId = od.OrderDetailId,
                OrderId = od.OrderId,
                ProductId = od.ProductId,
                ProductName = p?.ProductName ?? string.Empty,
                Barcode = p?.Barcode ?? string.Empty,
                Unit = p?.Unit ?? string.Empty,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                SubTotal = od.SubTotal
            };
        }).ToList();

        return new OrderDto
        {
            OrderId = order.OrderId,
            EmployeeId = order.EmployeeId,
            EmployeeName = employeeName,
            CustomerId = order.CustomerId,
            CustomerName = customerName,
            BranchId = order.BranchId,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            VoucherId = order.VoucherId,
            FinalAmount = order.FinalAmount,
            PaymentMethod = order.PaymentMethod,
            Status = order.Status,
            OrderDetails = detailDtos
        };
    }
}
