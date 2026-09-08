using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartSupermarket.Backend.Common.Results;
using SmartSupermarket.Backend.Features.Auth.DTOs;
using SmartSupermarket.Backend.Features.Auth.Services;

namespace SmartSupermarket.Backend.Features.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng nhập hệ thống cho tất cả các vai trò (Admin, Manager, Staff, Customer)
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(ApiResult<AuthResponse>.Success(response, "Đăng nhập thành công."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResult<AuthResponse>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<AuthResponse>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Đăng ký tài khoản Khách hàng mới (Public)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(ApiResult<AuthResponse>.Success(response, "Đăng ký tài khoản thành công."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<AuthResponse>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<AuthResponse>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Làm mới JWT Access Token bằng RefreshToken
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(ApiResult<AuthResponse>.Success(response, "Cap nhat Token thanh cong."));
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(ApiResult<AuthResponse>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<AuthResponse>.Failure(ex.Message));
        }
    }

    /// <summary>
    /// Lấy thông tin cá nhân của người dùng hiện tại (Yêu cầu Token)
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(ApiResult<UserProfileResponse>.Failure("Mã định danh Token không hợp lệ."));
            }

            var profile = await _authService.GetProfileAsync(userId);
            return Ok(ApiResult<UserProfileResponse>.Success(profile, "Lấy thông tin cá nhân thành công."));
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
    /// Admin tạo tài khoản nhân viên / quản lý mới (Yêu cầu quyền Admin)
    /// </summary>
    [HttpPost("create-staff")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        try
        {
            var staffProfile = await _authService.CreateStaffAsync(request);
            return Ok(ApiResult<UserProfileResponse>.Success(staffProfile, "Tạo tài khoản nhân viên thành công."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<UserProfileResponse>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<UserProfileResponse>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<UserProfileResponse>.Failure(ex.Message));
        }
    }
}
