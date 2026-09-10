using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Orders.DTOs;
using SmartSupermarket.Backend.Features.Orders.Services;

namespace SmartSupermarket.Backend.Features.Orders.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<OrderPagedResult>>> GetPaged(
        [FromQuery] int? employeeId,
        [FromQuery] int? customerId,
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetPagedAsync(employeeId, customerId, status, startDate, endDate, page, pageSize, cancellationToken);
        return Ok(ApiResult<OrderPagedResult>.Success(result, "Lấy danh sách đơn hàng thành công"));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<OrderDto>>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order == null)
        {
            return NotFound(ApiResult<OrderDto>.Failure($"Không tìm thấy đơn hàng có ID = {id}"));
        }
        return Ok(ApiResult<OrderDto>.Success(order, "Lấy thông tin đơn hàng thành công"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<OrderDto>>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _orderService.CreateOrderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.OrderId }, ApiResult<OrderDto>.Success(created, "Tạo đơn hàng thành công"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<OrderDto>.Failure(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<OrderDto>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<OrderDto>.Failure(ex.Message));
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ApiResult<bool>>> Cancel(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _orderService.CancelOrderAsync(id, cancellationToken);
            return Ok(ApiResult<bool>.Success(success, "Hủy đơn hàng thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
    }
}
