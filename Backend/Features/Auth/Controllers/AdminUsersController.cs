using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Auth.DTOs;
using SmartSupermarket.Backend.Features.Auth.Services;

namespace SmartSupermarket.Backend.Features.Auth.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public AdminUsersController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Admin lấy danh sách tài khoản người dùng (Hỗ trợ phân trang, lọc theo Role, BranchId, Tìm kiếm)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserRole? role,
        [FromQuery] int? branchId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var pagedResult = await _authService.GetUsersPagedAsync(role, branchId, search, page, pageSize);
            return Ok(ApiResult<PagedResult<UserProfileResponse>>.Success(pagedResult, "Lấy danh sách người dùng thành công."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<PagedResult<UserProfileResponse>>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Admin tạo tài khoản nhân viên / quản lý mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateStaffRequest request)
    {
        try
        {
            var userProfile = await _authService.CreateStaffAsync(request);
            return StatusCode(201, ApiResult<UserProfileResponse>.Success(userProfile, "Tạo tài khoản nhân viên thành công."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<UserProfileResponse>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<UserProfileResponse>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Admin cập nhật thông tin nhân viên theo ID
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var updatedProfile = await _authService.UpdateUserAsync(id, request);
            return Ok(ApiResult<UserProfileResponse>.Success(updatedProfile, "Cập nhật thông tin nhân viên thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<UserProfileResponse>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<UserProfileResponse>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Admin khóa mềm tài khoản nhân viên (Status = Locked)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> LockUser(int id)
    {
        try
        {
            await _authService.LockUserAsync(id);
            return Ok(ApiResult<bool>.Success(true, "Đã khóa tài khoản nhân viên thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResult<bool>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<bool>.Failure(ex.Message));
        }
    }
}
