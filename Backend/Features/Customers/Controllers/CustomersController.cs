using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Customers.DTOs;
using SmartSupermarket.Backend.Features.Customers.Services;

namespace SmartSupermarket.Backend.Features.Customers.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Tạo mới hồ sơ khách hàng thành viên.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResult<CustomerResponseDto>>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customerService.CreateAsync(request, cancellationToken);
            return Ok(ApiResult<CustomerResponseDto>.Success(customer, "Tạo khách hàng thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResult<CustomerResponseDto>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<CustomerResponseDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Tìm kiếm và phân trang danh sách khách hàng.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResult<CustomerPagedResponse<CustomerListItemDto>>>> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] string? search,
        [FromQuery] byte? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        string? queryKeyword = !string.IsNullOrWhiteSpace(keyword) ? keyword : search;
        var result = await _customerService.GetPagedAsync(queryKeyword, status, page, pageSize, cancellationToken);
        return Ok(ApiResult<CustomerPagedResponse<CustomerListItemDto>>.Success(result, "Lấy danh sách khách hàng thành công"));
    }

    /// <summary>
    /// Tra cứu nhanh khách hàng tại POS qua số điện thoại (?phone=...).
    /// </summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResult<CustomerLookupDto>>> Lookup(
        [FromQuery] string phone,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return BadRequest(ApiResult<CustomerLookupDto>.Failure("Vui lòng cung cấp số điện thoại."));
        }

        var customer = await _customerService.LookupByPhoneAsync(phone, cancellationToken);
        if (customer == null)
        {
            return NotFound(ApiResult<CustomerLookupDto>.Failure("Không tìm thấy khách hàng"));
        }

        return Ok(ApiResult<CustomerLookupDto>.Success(customer, "Tìm thấy khách hàng"));
    }

    /// <summary>
    /// Xem chi tiết khách hàng theo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<CustomerResponseDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound(ApiResult<CustomerResponseDto>.Failure($"Không tìm thấy Customer có ID = {id}"));
        }

        return Ok(ApiResult<CustomerResponseDto>.Success(customer, "Lấy thông tin khách hàng thành công"));
    }

    /// <summary>
    /// Cập nhật thông tin khách hàng.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<CustomerResponseDto>>> Update(
        int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _customerService.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResult<CustomerResponseDto>.Success(updated, "Cập nhật thông tin khách hàng thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CustomerResponseDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResult<CustomerResponseDto>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<CustomerResponseDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Xóa mềm khách hàng (chuyển sang INACTIVE).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult<bool>>> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _customerService.DeleteAsync(id, cancellationToken);
            return Ok(ApiResult<bool>.Success(true, "Chuyển trạng thái khách hàng sang INACTIVE thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Xem số dư điểm Loyalty của khách hàng.
    /// </summary>
    [HttpGet("{id:int}/loyalty")]
    public async Task<ActionResult<ApiResult<CustomerLoyaltyDto>>> GetLoyaltyPoints(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var loyalty = await _customerService.GetLoyaltyPointsAsync(id, cancellationToken);
            return Ok(ApiResult<CustomerLoyaltyDto>.Success(loyalty, "Lấy điểm thưởng thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CustomerLoyaltyDto>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Xem lịch sử biến động điểm Loyalty.
    /// </summary>
    [HttpGet("{id:int}/loyalty/history")]
    public async Task<ActionResult<ApiResult<PointHistoryListResponse>>> GetLoyaltyHistory(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _customerService.GetLoyaltyHistoryAsync(id, cancellationToken);
            return Ok(ApiResult<PointHistoryListResponse>.Success(history, "Lấy lịch sử điểm thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<PointHistoryListResponse>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Xem lịch sử đơn hàng của khách hàng.
    /// </summary>
    [HttpGet("{id:int}/orders")]
    public async Task<ActionResult<ApiResult<CustomerPagedResponse<CustomerOrderHistoryItemDto>>>> GetOrders(
        int id,
        [FromQuery] int? branchId,
        [FromQuery] byte? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orders = await _customerService.GetOrderHistoryAsync(
                id, branchId, status, startDate, endDate, page, pageSize, cancellationToken);
            return Ok(ApiResult<CustomerPagedResponse<CustomerOrderHistoryItemDto>>.Success(orders, "Lấy lịch sử mua hàng thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CustomerPagedResponse<CustomerOrderHistoryItemDto>>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Xem danh sách voucher của khách hàng.
    /// </summary>
    [HttpGet("{id:int}/vouchers")]
    public async Task<ActionResult<ApiResult<List<CustomerVoucherDto>>>> GetVouchers(
        int id,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var vouchers = await _customerService.GetVouchersAsync(id, status, cancellationToken);
            return Ok(ApiResult<List<CustomerVoucherDto>>.Success(vouchers, "Lấy danh sách voucher thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<List<CustomerVoucherDto>>.Failure(ex.Message));
        }
    }

    // ==========================================
    // Backward compatibility endpoints for Desktop POS
    // ==========================================
    [HttpGet("by-phone/{phone}")]
    public async Task<ActionResult<ApiResult<CustomerDto>>> GetByPhone(
        string phone,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByPhoneAsync(phone, cancellationToken);
        if (customer == null)
            return NotFound(ApiResult<CustomerDto>.Failure("Không tìm thấy khách hàng với số điện thoại này"));
        return Ok(ApiResult<CustomerDto>.Success(customer, "Tìm thấy khách hàng"));
    }

    [HttpPost("{id:int}/add-points")]
    public async Task<ActionResult<ApiResult<CustomerDto>>> AddPoints(
        int id,
        [FromBody] AddPointsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _customerService.AddPointsAsync(id, request, cancellationToken);
            return Ok(ApiResult<CustomerDto>.Success(updated, $"Cộng {request.PointsToAdd} điểm thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<CustomerDto>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<CustomerDto>.Failure(ex.Message));
        }
    }

    [HttpPost("{id:int}/redeem-voucher")]
    public async Task<ActionResult<ApiResult<RedeemVoucherResponse>>> RedeemVoucher(
        int id,
        [FromBody] RedeemVoucherRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _customerService.RedeemVoucherAsync(id, request, cancellationToken);
            if (!response.IsSuccess)
                return BadRequest(ApiResult<RedeemVoucherResponse>.Success(response, response.Message));
            return Ok(ApiResult<RedeemVoucherResponse>.Success(response, response.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<RedeemVoucherResponse>.Failure(ex.Message));
        }
    }
}
