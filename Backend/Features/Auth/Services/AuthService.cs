using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Auth.DTOs;
using SmartSupermarket.Backend.Features.Auth.Repositories;
using SmartSupermarket.Backend.Infrastructure.Security;

namespace SmartSupermarket.Backend.Features.Auth.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<UserProfileResponse> GetProfileAsync(int userId);
    Task<UserProfileResponse> CreateStaffAsync(CreateStaffRequest request);
    Task<PagedResult<UserProfileResponse>> GetUsersPagedAsync(UserRole? role, int? branchId, string? search, int page, int pageSize);
    Task<UserProfileResponse> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<bool> LockUserAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtService _jwtService;

    public AuthService(
        IUserRepository userRepository,
        PasswordHasher passwordHasher,
        JwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameOrPhoneOrEmailAsync(request.Identifier);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        if (user.Status == UserStatus.Locked)
        {
            throw new UnauthorizedAccessException("Tài khoản của bạn đã bị khóa.");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _userRepository.UpdateUser(user);
        await _userRepository.SaveChangesAsync();

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            BranchId = user.BranchId,
            ExpiresIn = _jwtService.Options.ExpiryMinutes * 60
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByUsernameOrPhoneOrEmailAsync(request.PhoneNumber);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Số điện thoại này đã được đăng ký tài khoản.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                throw new InvalidOperationException("Email này đã được sử dụng.");
            }
        }

        var user = new User
        {
            Username = request.PhoneNumber,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            Email = request.Email ?? $"{request.PhoneNumber}@customer.smartmarket.vn",
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Customer,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var customer = new Customer
        {
            User = user,
            LoyaltyPoints = 0,
            MembershipTier = 1
        };

        await _userRepository.AddUserAsync(user);
        await _userRepository.AddCustomerProfileAsync(customer);
        await _userRepository.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _userRepository.UpdateUser(user);
        await _userRepository.SaveChangesAsync();

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            BranchId = user.BranchId,
            ExpiresIn = _jwtService.Options.ExpiryMinutes * 60
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            throw new SecurityTokenException("AccessToken không hợp lệ.");
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            throw new SecurityTokenException("AccessToken không hợp lệ.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow || user.Status == UserStatus.Locked)
        {
            throw new SecurityTokenException("RefreshToken không hợp lệ hoặc đã hết hạn.");
        }

        var newAccessToken = _jwtService.GenerateToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _userRepository.UpdateUser(user);
        await _userRepository.SaveChangesAsync();

        return new AuthResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            BranchId = user.BranchId,
            ExpiresIn = _jwtService.Options.ExpiryMinutes * 60
        };
    }

    public async Task<UserProfileResponse> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");
        }

        return MapToUserProfileResponse(user);
    }

    public async Task<UserProfileResponse> CreateStaffAsync(CreateStaffRequest request)
    {
        var existingUser = await _userRepository.GetByUsernameOrPhoneOrEmailAsync(request.Username);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                throw new InvalidOperationException("Email đã được sử dụng.");
            }
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            Email = request.Email ?? $"{request.Username}@smartmarket.vn",
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            BranchId = request.BranchId,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddUserAsync(user);
        await _userRepository.SaveChangesAsync();

        return MapToUserProfileResponse(user);
    }

    public async Task<PagedResult<UserProfileResponse>> GetUsersPagedAsync(
        UserRole? role, int? branchId, string? search, int page, int pageSize)
    {
        var (users, totalCount) = await _userRepository.GetUsersPagedAsync(role, branchId, search, page, pageSize);

        var dtos = users.Select(MapToUserProfileResponse);

        return new PagedResult<UserProfileResponse>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserProfileResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");
        }

        user.FullName = request.FullName;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email;
        }
        user.PhoneNumber = request.PhoneNumber;
        user.Role = request.Role;
        user.Status = request.Status;
        user.BranchId = request.BranchId;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.UpdateUser(user);
        await _userRepository.SaveChangesAsync();

        return MapToUserProfileResponse(user);
    }

    public async Task<bool> LockUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");
        }

        user.Status = UserStatus.Locked;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.UpdateUser(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");
        }

        if (!_passwordHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu cũ không chính xác.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.UpdateUser(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    private static UserProfileResponse MapToUserProfileResponse(User user)
    {
        return new UserProfileResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            BranchId = user.BranchId,
            CustomerProfile = user.CustomerProfile != null
                ? new CustomerProfileDto
                {
                    CustomerId = user.CustomerProfile.CustomerId,
                    LoyaltyPoints = user.CustomerProfile.LoyaltyPoints,
                    MembershipTier = user.CustomerProfile.MembershipTier.ToString()
                }
                : null
        };
    }
}
