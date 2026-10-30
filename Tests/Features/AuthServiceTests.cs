using Microsoft.Extensions.Options;
using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Auth.DTOs;
using SmartSupermarket.Backend.Features.Auth.Repositories;
using SmartSupermarket.Backend.Features.Auth.Services;
using SmartSupermarket.Backend.Infrastructure.Security;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtService _jwtService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _passwordHasher = new PasswordHasher();

        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "SuperSecretKeyForSmartSupermarketProject2026!",
            Issuer = "SmartSupermarketAPI",
            Audience = "SmartSupermarketClients",
            ExpiryMinutes = 60
        });

        _jwtService = new JwtService(jwtOptions);
        _authService = new AuthService(_mockUserRepository.Object, _passwordHasher, _jwtService);
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        string rawPassword = "Password123!";
        string hashedPassword = _passwordHasher.HashPassword(rawPassword);

        var user = new User
        {
            UserId = 1,
            Username = "staff01",
            PasswordHash = hashedPassword,
            FullName = "Nguyễn Văn Staff",
            Role = UserRole.Staff,
            Status = UserStatus.Active
        };

        _mockUserRepository.Setup(r => r.GetByUsernameOrPhoneOrEmailAsync("staff01"))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(new LoginRequest { Identifier = "staff01", Password = rawPassword });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal("staff01", result.Username);
        Assert.Equal("Staff", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetByUsernameOrPhoneOrEmailAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest { Identifier = "nonexistent", Password = "Pass" }));
    }

    [Fact]
    public async Task LoginAsync_LockedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var user = new User { UserId = 1, Username = "lockeduser", Status = UserStatus.Locked };
        _mockUserRepository.Setup(r => r.GetByUsernameOrPhoneOrEmailAsync("lockeduser"))
            .ReturnsAsync(user);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest { Identifier = "lockeduser", Password = "Pass" }));
        Assert.Contains("khóa", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        string hashedPassword = _passwordHasher.HashPassword("CorrectPassword123!");
        var user = new User { UserId = 1, Username = "staff01", PasswordHash = hashedPassword, Status = UserStatus.Active };
        _mockUserRepository.Setup(r => r.GetByUsernameOrPhoneOrEmailAsync("staff01"))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest { Identifier = "staff01", Password = "WrongPassword" }));
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_ValidRequest_ShouldRegisterCustomerSuccessfully()
    {
        // Arrange
        var request = new RegisterRequest
        {
            PhoneNumber = "0987654321",
            FullName = "Khách Hàng Mới",
            Email = "khachhang@gmail.com",
            Password = "Password123!"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameOrPhoneOrEmailAsync("0987654321"))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("khachhang@gmail.com"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("0987654321", result.Username);
        _mockUserRepository.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Once);
        _mockUserRepository.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce());
    }

    [Fact]
    public async Task RegisterAsync_DuplicatePhone_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var existingUser = new User { UserId = 1, PhoneNumber = "0987654321" };
        _mockUserRepository.Setup(r => r.GetByUsernameOrPhoneOrEmailAsync("0987654321"))
            .ReturnsAsync(existingUser);

        var request = new RegisterRequest { PhoneNumber = "0987654321", Password = "Pass" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(request));
    }

    #endregion

    #region User Management Tests

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ShouldReturnUserProfile()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "admin01",
            FullName = "Quản trị viên",
            Email = "admin@smartmarket.vn",
            Role = UserRole.Admin,
            Status = UserStatus.Active
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetProfileAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal("admin01", result.Username);
        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task GetProfileAsync_NonExistingUser_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _authService.GetProfileAsync(999));
    }

    [Fact]
    public async Task LockUserAsync_ExistingUser_ShouldSetStatusToLocked()
    {
        // Arrange
        var user = new User { UserId = 5, Status = UserStatus.Active };
        _mockUserRepository.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LockUserAsync(5);

        // Assert
        Assert.True(result);
        Assert.Equal(UserStatus.Locked, user.Status);
        _mockUserRepository.Verify(r => r.UpdateUser(user), Times.Once);
        _mockUserRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion
}
