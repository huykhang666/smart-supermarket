using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Features.Payments.DTOs;
using SmartSupermarket.Backend.Features.Payments.Services;

namespace SmartSupermarket.Backend.Features.Payments.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentTransactionRequest request)
    {
        try
        {
            var result = await _paymentService.CreateTransactionAsync(request);
            return CreatedAtAction(nameof(GetTransaction), new { id = result.PaymentTransactionId }, new
            {
                isSuccess = true,
                message = "Tạo giao dịch thanh toán thành công",
                data = result,
                errors = (object?)null
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { isSuccess = false, message = ex.Message, data = (object?)null, errors = new[] { ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { isSuccess = false, message = "Lỗi hệ thống khi tạo thanh toán", data = (object?)null, errors = new[] { ex.Message } });
        }
    }

    [HttpGet("transactions/{id:int}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var result = await _paymentService.GetTransactionByIdAsync(id);
        if (result == null)
            return NotFound(new { isSuccess = false, message = "Không tìm thấy giao dịch thanh toán", data = (object?)null, errors = new[] { "Transaction not found" } });

        return Ok(new
        {
            isSuccess = true,
            message = "Lấy thông tin giao dịch thành công",
            data = result,
            errors = (object?)null
        });
    }

    [HttpGet("transactions/{id:int}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTransactionStatus(int id)
    {
        var result = await _paymentService.GetTransactionStatusAsync(id);
        if (result == null)
            return NotFound(new { isSuccess = false, message = "Không tìm thấy giao dịch", data = (object?)null, errors = new[] { "Transaction not found" } });

        return Ok(new
        {
            isSuccess = true,
            message = "Lấy trạng thái thanh toán thành công",
            data = result,
            errors = (object?)null
        });
    }

    [HttpPost("zalo-pay/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloPayCallbackPayload payload)
    {
        var result = await _paymentService.ProcessZaloPayCallbackAsync(payload);
        return Ok(result);
    }

    [HttpPost("transactions/{id:int}/cancel")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CancelTransaction(int id)
    {
        try
        {
            bool success = await _paymentService.CancelTransactionAsync(id);
            if (!success)
                return NotFound(new { isSuccess = false, message = "Không tìm thấy giao dịch", data = false, errors = new[] { "Transaction not found" } });

            return Ok(new
            {
                isSuccess = true,
                message = "Hủy giao dịch thanh toán thành công",
                data = true,
                errors = (object?)null
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { isSuccess = false, message = ex.Message, data = false, errors = new[] { ex.Message } });
        }
    }

    [HttpPost("transactions/{id:int}/sync-gateway")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> SyncGatewayStatus(int id)
    {
        try
        {
            var result = await _paymentService.SyncGatewayStatusAsync(id);
            return Ok(new
            {
                isSuccess = true,
                message = "Đồng bộ trạng thái cổng thanh toán thành công",
                data = result,
                errors = (object?)null
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { isSuccess = false, message = ex.Message, data = (object?)null, errors = new[] { ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { isSuccess = false, message = "Lỗi khi đồng bộ cổng thanh toán", data = (object?)null, errors = new[] { ex.Message } });
        }
    }

    [HttpGet("transactions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListTransactions([FromQuery] PaymentTransactionFilterParams filterParams)
    {
        var (items, totalItems) = await _paymentService.ListTransactionsAsync(filterParams);
        int totalPages = (int)Math.Ceiling((double)totalItems / filterParams.PageSize);

        return Ok(new
        {
            isSuccess = true,
            message = "Lấy danh sách giao dịch thành công",
            data = new
            {
                items,
                page = filterParams.Page,
                pageSize = filterParams.PageSize,
                totalItems,
                totalPages
            },
            errors = (object?)null
        });
    }
}
