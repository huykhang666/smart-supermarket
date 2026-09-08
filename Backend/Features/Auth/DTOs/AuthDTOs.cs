using System.ComponentModel.DataAnnotations;
using SmartSupermarket.Backend.Domain.Enums;

namespace SmartSupermarket.Backend.Features.Auth.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Tên đăng nhập, Email hoặc Số điện thoại là bắt buộc.")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string Password { get; set; } = string.Empty;
}

public class CreateStaffRequest
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Role là bắt buộc.")]
    public UserRole Role { get; set; } = UserRole.Staff;

    public int? BranchId { get; set; }
}

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Staff;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public int? BranchId { get; set; }
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Mật khẩu cũ là bắt buộc.")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "AccessToken là bắt buộc.")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "RefreshToken là bắt buộc.")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public int ExpiresIn { get; set; }
}

public class UserProfileResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public CustomerProfileDto? CustomerProfile { get; set; }
}

public class CustomerProfileDto
{
    public int CustomerId { get; set; }
    public int LoyaltyPoints { get; set; }
    public string MembershipTier { get; set; } = string.Empty;
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
