using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Payments.DTOs;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Payments.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly IZaloPayService _zaloPayService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext context, IZaloPayService zaloPayService, ILogger<PaymentService> logger)
    {
        _context = context;
        _zaloPayService = zaloPayService;
        _logger = logger;
    }

    public async Task<PaymentTransactionResponse> CreateTransactionAsync(CreatePaymentTransactionRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

        if (order == null)
            throw new InvalidOperationException($"Không tìm thấy đơn hàng với OrderId = {request.OrderId}");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0");

        decimal paidTotal = order.Payments.Sum(p => p.AmountPaid);
        if (paidTotal >= order.FinalAmount)
            throw new InvalidOperationException("Đơn hàng này đã được thanh toán đầy đủ");

        string appTransId = $"{DateTime.UtcNow:yyMMdd}_{order.OrderId}_{Random.Shared.Next(1000, 9999)}";

        var transaction = new PaymentTransaction
        {
            TransactionCode = appTransId,
            OrderId = order.OrderId,
            PaymentMethod = request.PaymentMethod,
            Amount = request.Amount,
            Gateway = "ZALOPAY",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow,
            ExpiredAt = DateTime.UtcNow.AddMinutes(15)
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        if (request.PaymentMethod == PaymentMethod.QRCode)
        {
            var (isSuccess, orderUrl, qrCode, error) = await _zaloPayService.CreateOrderAsync(
                appTransId,
                request.Amount,
                $"Thanh toan don hang #{order.OrderId}"
            );

            if (isSuccess)
            {
                transaction.Status = "PROCESSING";
                transaction.PaymentUrl = orderUrl;
                transaction.QrCode = qrCode;
            }
            else
            {
                transaction.Status = "FAILED";
                _logger.LogError("Gửi yêu cầu ZaloPay thất bại cho transaction {Code}: {Error}", appTransId, error);
            }

            await _context.SaveChangesAsync();
        }

        return MapToResponse(transaction);
    }

    public async Task<PaymentTransactionResponse?> GetTransactionByIdAsync(int id)
    {
        var transaction = await _context.PaymentTransactions.FirstOrDefaultAsync(pt => pt.PaymentTransactionId == id);
        return transaction == null ? null : MapToResponse(transaction);
    }

    public async Task<PaymentStatusResponse?> GetTransactionStatusAsync(int id)
    {
        var transaction = await _context.PaymentTransactions.FirstOrDefaultAsync(pt => pt.PaymentTransactionId == id);
        if (transaction == null) return null;

        return new PaymentStatusResponse
        {
            PaymentTransactionId = transaction.PaymentTransactionId,
            Status = transaction.Status,
            PaidAt = transaction.PaidAt
        };
    }

    public async Task<ZaloPayCallbackResponse> ProcessZaloPayCallbackAsync(ZaloPayCallbackPayload payload)
    {
        if (!_zaloPayService.VerifyCallback(payload.Data, payload.Mac))
        {
            _logger.LogWarning("ZaloPay Callback HMAC verification failed");
            return new ZaloPayCallbackResponse { ReturnCode = -1, ReturnMessage = "mac not equal" };
        }

        ZaloPayCallbackData? callbackData;
        try
        {
            callbackData = JsonSerializer.Deserialize<ZaloPayCallbackData>(payload.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi parse JSON ZaloPay Callback Data");
            return new ZaloPayCallbackResponse { ReturnCode = -1, ReturnMessage = "invalid json data" };
        }

        if (callbackData == null || string.IsNullOrEmpty(callbackData.AppTransId))
        {
            return new ZaloPayCallbackResponse { ReturnCode = -1, ReturnMessage = "missing app_trans_id" };
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(pt => pt.TransactionCode == callbackData.AppTransId);

            if (transaction == null)
            {
                _logger.LogWarning("ZaloPay Callback: Không tìm thấy transaction {AppTransId}", callbackData.AppTransId);
                return new ZaloPayCallbackResponse { ReturnCode = 0, ReturnMessage = "transaction not found" };
            }

            // Kiểm tra tính Idempotent
            if (transaction.Status == "SUCCESS")
            {
                _logger.LogInformation("ZaloPay Callback: Transaction {AppTransId} đã xử lý trước đó", callbackData.AppTransId);
                return new ZaloPayCallbackResponse { ReturnCode = 1, ReturnMessage = "success (duplicate)" };
            }

            transaction.Status = "SUCCESS";
            transaction.PaidAt = DateTime.UtcNow;
            transaction.GatewayTransactionId = callbackData.ZpTransId.ToString();

            // Tạo bản ghi Payment chính thức
            var payment = new Domain.Entities.Payment
            {
                OrderId = transaction.OrderId,
                PaymentMethod = transaction.PaymentMethod,
                AmountPaid = transaction.Amount,
                PaymentDate = DateTime.UtcNow
            };
            _context.Payments.Add(payment);

            // Kiểm tra tổng tiền đã trả cho Order
            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == transaction.OrderId);

            if (order != null)
            {
                decimal totalPaid = order.Payments.Sum(p => p.AmountPaid) + payment.AmountPaid;
                if (totalPaid >= order.FinalAmount)
                {
                    order.Status = OrderStatus.Completed;
                }
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("Ghi nhận thanh toán thành công cho đơn {OrderId} qua ZaloPay trans {Code}", transaction.OrderId, transaction.TransactionCode);
            return new ZaloPayCallbackResponse { ReturnCode = 1, ReturnMessage = "success" };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi xử lý Callback ZaloPay cho trans {Code}", callbackData.AppTransId);
            return new ZaloPayCallbackResponse { ReturnCode = 0, ReturnMessage = ex.Message };
        }
    }

    public async Task<bool> CancelTransactionAsync(int id)
    {
        var transaction = await _context.PaymentTransactions.FirstOrDefaultAsync(pt => pt.PaymentTransactionId == id);
        if (transaction == null) return false;

        if (transaction.Status == "SUCCESS")
            throw new InvalidOperationException("Không thể hủy giao dịch đã thanh toán thành công");

        transaction.Status = "CANCELLED";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SyncGatewayStatusResponse> SyncGatewayStatusAsync(int id)
    {
        var transaction = await _context.PaymentTransactions.FirstOrDefaultAsync(pt => pt.PaymentTransactionId == id);
        if (transaction == null)
            throw new InvalidOperationException($"Không tìm thấy transaction với ID = {id}");

        if (transaction.Status == "SUCCESS")
        {
            return new SyncGatewayStatusResponse
            {
                PaymentTransactionId = transaction.PaymentTransactionId,
                Status = "SUCCESS",
                GatewayResponseCode = 1,
                Message = "Giao dịch đã ở trạng thái thành công"
            };
        }

        var (returnCode, returnMessage, zpTransId) = await _zaloPayService.QueryOrderStatusAsync(transaction.TransactionCode);

        if (returnCode == 1) // Thanh toán thành công trên Gateway
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                transaction.Status = "SUCCESS";
                transaction.PaidAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(zpTransId))
                    transaction.GatewayTransactionId = zpTransId;

                var payment = new Domain.Entities.Payment
                {
                    OrderId = transaction.OrderId,
                    PaymentMethod = transaction.PaymentMethod,
                    AmountPaid = transaction.Amount,
                    PaymentDate = DateTime.UtcNow
                };
                _context.Payments.Add(payment);

                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.OrderId == transaction.OrderId);

                if (order != null)
                {
                    decimal totalPaid = order.Payments.Sum(p => p.AmountPaid) + payment.AmountPaid;
                    if (totalPaid >= order.FinalAmount)
                    {
                        order.Status = OrderStatus.Completed;
                    }
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new SyncGatewayStatusResponse
                {
                    PaymentTransactionId = transaction.PaymentTransactionId,
                    Status = "SUCCESS",
                    GatewayResponseCode = 1,
                    Message = "Đồng bộ thành công: Giao dịch ZaloPay đã hoàn tất"
                };
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        return new SyncGatewayStatusResponse
        {
            PaymentTransactionId = transaction.PaymentTransactionId,
            Status = transaction.Status,
            GatewayResponseCode = returnCode,
            Message = returnMessage
        };
    }

    public async Task<(IEnumerable<PaymentTransactionResponse> items, int totalItems)> ListTransactionsAsync(PaymentTransactionFilterParams filterParams)
    {
        var query = _context.PaymentTransactions.AsQueryable();

        if (filterParams.OrderId.HasValue)
            query = query.Where(pt => pt.OrderId == filterParams.OrderId.Value);

        if (!string.IsNullOrWhiteSpace(filterParams.Gateway))
            query = query.Where(pt => pt.Gateway == filterParams.Gateway);

        if (!string.IsNullOrWhiteSpace(filterParams.Status))
            query = query.Where(pt => pt.Status == filterParams.Status);

        if (filterParams.PaymentMethod.HasValue)
            query = query.Where(pt => pt.PaymentMethod == filterParams.PaymentMethod.Value);

        if (filterParams.StartDate.HasValue)
            query = query.Where(pt => pt.CreatedAt >= filterParams.StartDate.Value);

        if (filterParams.EndDate.HasValue)
            query = query.Where(pt => pt.CreatedAt <= filterParams.EndDate.Value);

        int totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(pt => pt.CreatedAt)
            .Skip((filterParams.Page - 1) * filterParams.PageSize)
            .Take(filterParams.PageSize)
            .ToListAsync();

        return (items.Select(MapToResponse), totalItems);
    }

    private static PaymentTransactionResponse MapToResponse(PaymentTransaction pt)
    {
        return new PaymentTransactionResponse
        {
            PaymentTransactionId = pt.PaymentTransactionId,
            TransactionCode = pt.TransactionCode,
            OrderId = pt.OrderId,
            PaymentMethod = pt.PaymentMethod,
            Gateway = pt.Gateway,
            Amount = pt.Amount,
            Status = pt.Status,
            PaymentUrl = pt.PaymentUrl,
            QrCode = pt.QrCode,
            CreatedAt = pt.CreatedAt,
            PaidAt = pt.PaidAt
        };
    }
}
