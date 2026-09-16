using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Customers.DTOs;
using SmartSupermarket.Backend.Features.Customers.Services;

namespace SmartSupermarket.Backend.Features.Customers.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<CustomerPagedResult>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetPagedAsync(search, page, pageSize, cancellationToken);
        return Ok(ApiResult<CustomerPagedResult>.Success(result, "Lấy danh sách khách hàng thành công"));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<CustomerDto>>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer == null)
            return NotFound(ApiResult<CustomerDto>.Failure($"Không tìm thấy khách hàng có ID = {id}"));
        return Ok(ApiResult<CustomerDto>.Success(customer, "Lấy thông tin khách hàng thành công"));
    }

    [HttpGet("by-phone/{phone}")]
    public async Task<ActionResult<ApiResult<CustomerDto>>> GetByPhone(string phone, CancellationToken cancellationToken = default)
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
