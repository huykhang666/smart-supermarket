using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Employees.DTOs;
using SmartSupermarket.Backend.Features.Employees.Services;

namespace SmartSupermarket.Backend.Features.Employees.Controllers;

[ApiController]
[Route("api/v1/shifts/sessions")]
public class ShiftSessionsController : ControllerBase
{
    private readonly IShiftSessionService _sessionService;

    public ShiftSessionsController(IShiftSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// Mở phiên ca trực bán hàng mới & khai báo tiền mặt đầu ca
    /// </summary>
    [HttpPost("open")]
    public async Task<ActionResult<ApiResult<ShiftSessionResponse>>> OpenSession(
        [FromBody] OpenShiftSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sessionService.OpenSessionAsync(request, cancellationToken);
            return Ok(ApiResult<ShiftSessionResponse>.Success(result, "Mở ca làm việc thành công."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<ShiftSessionResponse>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Lấy thông tin ca trực hiện tại của nhân viên kèm doanh thu thời gian thực
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResult<ShiftSessionResponse>>> GetCurrentSession(
        [FromQuery] int employeeId,
        [FromQuery] int branchId = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.GetCurrentSessionAsync(employeeId, branchId, cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResult<ShiftSessionResponse>.Failure("Nhân viên hiện chưa mở ca trực nào."));
        }
        return Ok(ApiResult<ShiftSessionResponse>.Success(result, "Lấy thông tin ca trực hiện tại thành công."));
    }

    /// <summary>
    /// Chốt ca trực & kiểm đếm tiền mặt két (Yêu cầu giải trình nếu có chênh lệch)
    /// </summary>
    [HttpPost("close")]
    public async Task<ActionResult<ApiResult<ShiftSessionResponse>>> CloseSession(
        [FromBody] CloseShiftSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sessionService.CloseSessionAsync(request, cancellationToken);
            return Ok(ApiResult<ShiftSessionResponse>.Success(result, "Chốt và đóng ca bán hàng thành công."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<ShiftSessionResponse>.Failure(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<ShiftSessionResponse>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<ShiftSessionResponse>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Lấy lịch sử các ca trực đã chốt để Admin/Manager đối soát
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResult<List<ShiftSessionResponse>>>> GetHistory(
        [FromQuery] int branchId = 1,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.GetSessionHistoryAsync(branchId, fromDate, toDate, cancellationToken);
        return Ok(ApiResult<List<ShiftSessionResponse>>.Success(result, "Lấy lịch sử ca trực thành công."));
    }

    /// <summary>
    /// Giám sát thời gian thực danh sách máy trạm POS & Nhân viên đang online
    /// </summary>
    [HttpGet("online-workstations")]
    public async Task<ActionResult<ApiResult<List<OnlineWorkstationResponse>>>> GetOnlineWorkstations(
        [FromQuery] int branchId = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.GetOnlineWorkstationsAsync(branchId, cancellationToken);
        return Ok(ApiResult<List<OnlineWorkstationResponse>>.Success(result, "Lấy danh sách máy trạm trực tuyến thành công."));
    }

    /// <summary>
    /// Cập nhật nhịp tim định kỳ (Heartbeat) từ máy trạm POS
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult<ApiResult<bool>>> Heartbeat(
        [FromBody] HeartbeatRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await _sessionService.UpdateHeartbeatAsync(request, cancellationToken);
        return Ok(ApiResult<bool>.Success(success, "Heartbeat OK"));
    }

    /// <summary>
    /// Xác thực nhanh mã PIN quản lý để duyệt thao tác nhạy cảm tại POS
    /// </summary>
    [HttpPost("verify-manager-pin")]
    public async Task<ActionResult<ApiResult<bool>>> VerifyManagerPin(
        [FromBody] VerifyManagerPinRequest request,
        CancellationToken cancellationToken = default)
    {
        bool isValid = await _sessionService.VerifyManagerPinAsync(request.Pin, cancellationToken);
        if (!isValid)
        {
            return Unauthorized(ApiResult<bool>.Failure("Mã PIN Quản lý không chính xác!"));
        }
        return Ok(ApiResult<bool>.Success(true, "Xác thực mã PIN Quản lý thành công."));
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các đơn hàng đã bán trong phiên ca để đối soát két tiền
    /// </summary>
    [HttpGet("{sessionId:int}/orders")]
    public async Task<ActionResult<ApiResult<List<ShiftOrderDto>>>> GetSessionOrders(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.GetSessionOrdersAsync(sessionId, cancellationToken);
        return Ok(ApiResult<List<ShiftOrderDto>>.Success(result, "Lấy danh sách đơn hàng ca trực thành công."));
    }
}
