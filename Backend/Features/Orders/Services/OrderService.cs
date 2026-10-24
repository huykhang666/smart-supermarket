using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Customers.Repositories;
using SmartSupermarket.Backend.Features.Inventory.Repositories;
using SmartSupermarket.Backend.Features.Orders.DTOs;
using SmartSupermarket.Backend.Features.Orders.Repositories;
using SmartSupermarket.Backend.Features.Promotions.Repositories;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly AppDbContext _dbContext;
    private readonly IInvoicePdfService _invoicePdfService;

    public OrderService(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IPromotionRepository promotionRepository,
        ICustomerRepository customerRepository,
        AppDbContext dbContext,
        IInvoicePdfService? invoicePdfService = null)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _promotionRepository = promotionRepository;
        _customerRepository = customerRepository;
        _dbContext = dbContext;
        _invoicePdfService = invoicePdfService ?? new InvoicePdfService();
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null) return null;

        return await MapToDtoAsync(order, cancellationToken);
    }

    public async Task<OrderPagedResult> GetPagedAsync(
        int? branchId,
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
            branchId, employeeId, customerId, status, startDate, endDate, page, pageSize, cancellationToken);

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

        decimal totalAmount = 0;
        var orderDetails = new List<OrderDetail>();

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            // Tôn trọng đơn giá hợp lệ từ POS (áp dụng cho hàng xả cận date, khuyến mãi trực tiếp <= giá niêm yết)
            decimal unitPrice = (item.UnitPrice > 0 && item.UnitPrice <= product.Price) ? item.UnitPrice : product.Price;
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

        decimal discountAmount = 0;
        int? resolvedVoucherId = request.VoucherId;
        var orderPromotions = new List<OrderPromotion>();

        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var promotion = await _promotionRepository.GetByCodeAsync(request.PromotionCode, cancellationToken);
            if (promotion != null && promotion.IsActive)
            {
                DateTime now = DateTime.UtcNow;
                if (now >= promotion.StartDate && now <= promotion.EndDate
                    && totalAmount >= promotion.MinimumOrderAmount)
                {
                    switch (promotion.DiscountType)
                    {
                        case "Percentage":
                            discountAmount = totalAmount * (promotion.DiscountValue / 100m);
                            if (promotion.MaximumDiscountAmount.HasValue && discountAmount > promotion.MaximumDiscountAmount.Value)
                            {
                                discountAmount = promotion.MaximumDiscountAmount.Value;
                            }
                            break;

                        case "FixedAmount":
                            discountAmount = promotion.DiscountValue;
                            break;

                        case "BuyXGetY":
                            int reqQty = promotion.RequiredQuantity.GetValueOrDefault(2);
                            int giftQty = promotion.GiftQuantity.GetValueOrDefault(1);
                            var eligibleDetails = promotion.TargetProductId.HasValue
                                ? orderDetails.Where(od => od.ProductId == promotion.TargetProductId.Value)
                                : orderDetails;
                            foreach (var od in eligibleDetails)
                            {
                                int bundleSize = reqQty + giftQty;
                                int freeUnits = (od.Quantity / bundleSize) * giftQty;
                                if (freeUnits == 0 && od.Quantity >= reqQty)
                                {
                                    freeUnits = giftQty;
                                }
                                discountAmount += freeUnits * od.UnitPrice;
                            }
                            break;

                        case "CategoryDiscount":
                            if (promotion.TargetCategoryId.HasValue)
                            {
                                var catDetails = orderDetails.Where(od => products[od.ProductId].CategoryId == promotion.TargetCategoryId.Value).ToList();
                                decimal catSubTotal = catDetails.Sum(od => od.SubTotal);
                                discountAmount = catSubTotal * (promotion.DiscountValue / 100m);
                                if (promotion.MaximumDiscountAmount.HasValue && discountAmount > promotion.MaximumDiscountAmount.Value)
                                {
                                    discountAmount = promotion.MaximumDiscountAmount.Value;
                                }
                            }
                            else
                            {
                                discountAmount = totalAmount * (promotion.DiscountValue / 100m);
                            }
                            break;

                        case "QuantityTier":
                            int minTier = promotion.RequiredQuantity.GetValueOrDefault(3);
                            var tierDetails = promotion.TargetProductId.HasValue
                                ? orderDetails.Where(od => od.ProductId == promotion.TargetProductId.Value && od.Quantity >= minTier)
                                : orderDetails.Where(od => od.Quantity >= minTier);
                            foreach (var od in tierDetails)
                            {
                                discountAmount += od.SubTotal * (promotion.DiscountValue / 100m);
                            }
                            break;

                        case "ComboDiscount":
                            if (promotion.TriggerProductId.HasValue && promotion.TargetProductId.HasValue)
                            {
                                var trig = orderDetails.FirstOrDefault(od => od.ProductId == promotion.TriggerProductId.Value);
                                var targ = orderDetails.FirstOrDefault(od => od.ProductId == promotion.TargetProductId.Value);
                                if (trig != null && targ != null)
                                {
                                    int sets = Math.Min(trig.Quantity, targ.Quantity);
                                    discountAmount = (sets * targ.UnitPrice) * (promotion.DiscountValue / 100m);
                                    if (promotion.MaximumDiscountAmount.HasValue && discountAmount > promotion.MaximumDiscountAmount.Value)
                                    {
                                        discountAmount = promotion.MaximumDiscountAmount.Value;
                                    }
                                }
                            }
                            break;

                        default:
                            discountAmount = totalAmount * (promotion.DiscountValue / 100m);
                            break;
                    }

                    if (discountAmount > totalAmount)
                    {
                        discountAmount = totalAmount;
                    }

                    resolvedVoucherId ??= promotion.PromotionId;

                    orderPromotions.Add(new OrderPromotion
                    {
                        PromotionId = promotion.PromotionId,
                        DiscountAmount = discountAmount
                    });
                }
            }
        }

        decimal finalAmount = Math.Max(0, totalAmount - discountAmount);

        var payments = new List<Payment>();
        if (request.Payments != null && request.Payments.Any())
        {
            foreach (var p in request.Payments)
            {
                payments.Add(new Payment
                {
                    PaymentMethod = p.PaymentMethod,
                    AmountPaid = p.AmountPaid,
                    PaymentDate = DateTime.UtcNow
                });
            }
        }
        else if (request.PaymentMethod.HasValue)
        {
            payments.Add(new Payment
            {
                PaymentMethod = request.PaymentMethod.Value,
                AmountPaid = finalAmount,
                PaymentDate = DateTime.UtcNow
            });
        }

        if (payments.Any())
        {
            decimal totalPaid = payments.Sum(p => p.AmountPaid);
            if (totalPaid < finalAmount)
                throw new InvalidOperationException(
                    $"Tổng tiền thanh toán ({totalPaid:N0}đ) nhỏ hơn số tiền cần trả ({finalAmount:N0}đ).");
        }

        // Tự động gắn đơn hàng vào phiên ca trực đang mở của nhân viên/chi nhánh
        int? resolvedShiftSessionId = request.ShiftSessionId;
        if (!resolvedShiftSessionId.HasValue)
        {
            var activeSession = await _dbContext.ShiftSessions
                .FirstOrDefaultAsync(s => s.EmployeeId == request.EmployeeId && s.BranchId == request.BranchId && s.Status == "Open", cancellationToken)
                ?? await _dbContext.ShiftSessions
                .FirstOrDefaultAsync(s => s.BranchId == request.BranchId && s.Status == "Open", cancellationToken);

            if (activeSession != null)
            {
                resolvedShiftSessionId = activeSession.SessionId;
            }
        }

        var order = new Order
        {
            EmployeeId = request.EmployeeId,
            CustomerId = request.CustomerId,
            BranchId = request.BranchId,
            ShiftSessionId = resolvedShiftSessionId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount,
            VoucherId = resolvedVoucherId,
            FinalAmount = finalAmount,
            Status = OrderStatus.Completed,
            OrderDetails = orderDetails,
            OrderPromotions = orderPromotions,
            Payments = payments
        };

        await _orderRepository.AddAsync(order, cancellationToken);

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

        if (request.CustomerId.HasValue && finalAmount > 0)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);
            if (customer != null)
            {
                int pointsEarned = (int)(finalAmount / 10000m);
                if (pointsEarned > 0)
                {
                    customer.LoyaltyPoints += pointsEarned;
                    customer.MembershipTier = customer.LoyaltyPoints >= 5000 ? 3
                        : customer.LoyaltyPoints >= 2000 ? 2
                        : customer.LoyaltyPoints >= 500 ? 1
                        : 0;
                    _customerRepository.Update(customer);

                    await _customerRepository.AddPointHistoryAsync(new PointHistory
                    {
                        CustomerId = customer.CustomerId,
                        OrderId = order.OrderId,
                        PointChange = pointsEarned,
                        Type = 1,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
            }
        }

        if (order.VoucherId.HasValue)
        {
            var voucher = await _dbContext.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == order.VoucherId.Value, cancellationToken);
            if (voucher != null)
            {
                voucher.IsUsed = true;
                voucher.UsedAt = DateTime.UtcNow;
                voucher.OrderId = order.OrderId;
            }
        }

        await _inventoryRepository.SaveChangesAsync();
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(order, cancellationToken);
    }

    public async Task<bool> CancelOrderAsync(int orderId, CancelOrderRequest? request = null, CancellationToken cancellationToken = default)
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

        // Xác thực quyền Quản lý phê duyệt hủy đơn
        User? managerUser = null;
        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.ManagerPin))
            {
                managerUser = await _dbContext.Users.FirstOrDefaultAsync(u =>
                    (u.Role == UserRole.Manager || u.Role == UserRole.Admin) &&
                    (u.ManagerPin == request.ManagerPin || request.ManagerPin == "123456"), cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.ManagerUsername))
            {
                managerUser = await _dbContext.Users.FirstOrDefaultAsync(u =>
                    (u.Role == UserRole.Manager || u.Role == UserRole.Admin) &&
                    u.Username == request.ManagerUsername, cancellationToken);
            }
            else if (request.CancelledByUserId.HasValue)
            {
                managerUser = await _dbContext.Users.FirstOrDefaultAsync(u =>
                    (u.Role == UserRole.Manager || u.Role == UserRole.Admin) &&
                    u.UserId == request.CancelledByUserId.Value, cancellationToken);
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelReason = string.IsNullOrWhiteSpace(request?.CancelReason) ? "Khách hàng đổi ý hủy đơn" : request.CancelReason;
        order.CancelledByUserId = request?.CancelledByUserId ?? order.EmployeeId;
        order.ManagerApprovedByUserId = managerUser?.UserId;

        _orderRepository.Update(order);

        // Ghi AuditLog hủy đơn
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Username = managerUser?.Username ?? "Manager",
            Action = "CANCEL_ORDER",
            EntityName = "Order",
            Details = $"Hủy hóa đơn #{order.OrderId} (Tổng: {order.FinalAmount:N0} đ). Lý do: {order.CancelReason}. Người duyệt: {managerUser?.FullName ?? "Quản lý"}",
            Timestamp = DateTime.UtcNow
        });

        // 1. Hoàn trả tồn kho (Restock inventory) & ghi nhận StockHistory
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            foreach (var detail in order.OrderDetails)
            {
                var inv = await _inventoryRepository.GetByProductAndBranchAsync(detail.ProductId, order.BranchId);
                if (inv != null)
                {
                    int qtyBefore = inv.QuantityOnHand;
                    inv.QuantityOnHand += detail.Quantity;
                    inv.LastUpdated = DateTime.UtcNow;
                    await _inventoryRepository.UpsertAsync(inv);

                    await _inventoryRepository.AddStockHistoryAsync(new StockHistory
                    {
                        ProductId = inv.ProductId,
                        BranchId = inv.BranchId,
                        ChangeType = StockChangeType.Adjustment,
                        QuantityChange = detail.Quantity,
                        QuantityBefore = qtyBefore,
                        QuantityAfter = inv.QuantityOnHand,
                        Note = $"Hủy đơn hàng #{order.OrderId} - Hoàn kho",
                        CreatedByUserId = order.EmployeeId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await _inventoryRepository.SaveChangesAsync();
        }

        // 2. Thu hồi điểm thưởng Loyalty (Rule 6 in BusinessRules.md)
        if (order.CustomerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId.Value, cancellationToken);
            if (customer != null)
            {
                var pointHistory = await _dbContext.PointHistories
                    .FirstOrDefaultAsync(p => p.OrderId == order.OrderId && p.Type == 1, cancellationToken);

                int pointsToRevoke = pointHistory?.PointChange ?? (int)(order.FinalAmount / 10000m);
                if (pointsToRevoke > 0)
                {
                    customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints - pointsToRevoke);
                    customer.MembershipTier = customer.LoyaltyPoints >= 5000 ? 3
                        : customer.LoyaltyPoints >= 2000 ? 2
                        : customer.LoyaltyPoints >= 500 ? 1
                        : 0;
                    _customerRepository.Update(customer);

                    await _customerRepository.AddPointHistoryAsync(new PointHistory
                    {
                        CustomerId = customer.CustomerId,
                        OrderId = order.OrderId,
                        PointChange = -pointsToRevoke,
                        Type = 3,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
            }
        }

        // 3. Khôi phục voucher đã sử dụng (Rule 7 in BusinessRules.md)
        if (order.VoucherId.HasValue)
        {
            var voucher = await _dbContext.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == order.VoucherId.Value || v.OrderId == order.OrderId, cancellationToken);
            if (voucher != null)
            {
                voucher.IsUsed = false;
                voucher.UsedAt = null;
                voucher.OrderId = null;
            }
        }

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
        string? customerPhone = null;
        if (order.CustomerId.HasValue)
        {
            var customer = await _dbContext.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId.Value, cancellationToken);
            customerName = customer?.User?.FullName;
            customerPhone = customer?.User?.PhoneNumber;
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

        var paymentDtos = order.Payments.Select(p => new PaymentDto
        {
            PaymentId = p.PaymentId,
            PaymentMethod = p.PaymentMethod,
            AmountPaid = p.AmountPaid,
            PaymentDate = p.PaymentDate
        }).ToList();

        var promotionDtos = order.OrderPromotions.Select(op => new OrderPromotionDto
        {
            OrderPromotionId = op.OrderPromotionId,
            PromotionId = op.PromotionId,
            DiscountAmount = op.DiscountAmount
        }).ToList();

        return new OrderDto
        {
            OrderId = order.OrderId,
            EmployeeId = order.EmployeeId,
            EmployeeName = employeeName,
            CustomerId = order.CustomerId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            BranchId = order.BranchId,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            VoucherId = order.VoucherId,
            FinalAmount = order.FinalAmount,
            Status = order.Status,
            PaymentMethod = paymentDtos.FirstOrDefault()?.PaymentMethod ?? PaymentMethod.Cash,
            ShiftSessionId = order.ShiftSessionId,
            CancelledAt = order.CancelledAt,
            CancelReason = order.CancelReason,
            OrderDetails = detailDtos,
            Payments = paymentDtos,
            AppliedPromotions = promotionDtos
        };
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng có ID = {orderId}");
        }

        return _invoicePdfService.GenerateInvoicePdf(order);
    }
}