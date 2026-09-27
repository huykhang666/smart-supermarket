using Moq;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Auth.Repositories;
using SmartSupermarket.Backend.Features.Customers.DTOs;
using SmartSupermarket.Backend.Features.Customers.Repositories;
using SmartSupermarket.Backend.Features.Customers.Services;
using SmartSupermarket.Backend.Infrastructure.Security;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly PasswordHasher _passwordHasher;
    private readonly CustomerService _customerService;

    public CustomerServiceTests()
    {
        _mockCustomerRepo = new Mock<ICustomerRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _passwordHasher = new PasswordHasher();

        _customerService = new CustomerService(
            _mockCustomerRepo.Object,
            _mockUserRepo.Object,
            _passwordHasher);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidData_ShouldCreateCustomerWithZeroPoints()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FullName = "Nguyễn Văn An",
            Phone = "0901234567",
            Email = "an@example.com",
            DateOfBirth = new DateTime(2000, 5, 20),
            Gender = 1,
            Address = "TP. Hồ Chí Minh"
        };

        _mockCustomerRepo.Setup(r => r.GetActiveByPhoneAsync("0901234567", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _mockUserRepo.Setup(r => r.GetByPhoneAsync("0901234567"))
            .ReturnsAsync((User?)null);

        Customer? savedCustomer = null;
        _mockCustomerRepo.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) =>
            {
                c.CustomerId = 1;
                savedCustomer = c;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _customerService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.CustomerId);
        Assert.Equal("Nguyễn Văn An", result.FullName);
        Assert.Equal("0901234567", result.Phone);
        Assert.Equal("an@example.com", result.Email);
        Assert.Equal(0, result.LoyaltyPoints);
        Assert.Equal(1, result.Status); // ACTIVE
        Assert.Equal("TP. Hồ Chí Minh", result.Address);
        Assert.Equal((byte)1, result.Gender);

        _mockCustomerRepo.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCustomerRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyFullName_ShouldThrowArgumentException(string fullName)
    {
        var request = new CreateCustomerRequest
        {
            FullName = fullName,
            Phone = "0901234567"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _customerService.CreateAsync(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("abcdefghij")]
    public async Task CreateAsync_InvalidPhone_ShouldThrowArgumentException(string phone)
    {
        var request = new CreateCustomerRequest
        {
            FullName = "Nguyễn Văn An",
            Phone = phone
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _customerService.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_ShouldThrowArgumentException()
    {
        var request = new CreateCustomerRequest
        {
            FullName = "Nguyễn Văn An",
            Phone = "0901234567",
            Email = "invalid-email"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _customerService.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_DuplicateActivePhone_ShouldThrowInvalidOperationException()
    {
        var request = new CreateCustomerRequest
        {
            FullName = "Nguyễn Văn An",
            Phone = "0901234567"
        };

        var existingCustomer = new Customer
        {
            CustomerId = 99,
            Status = 1,
            User = new User { PhoneNumber = "0901234567", FullName = "Existing User" }
        };

        _mockCustomerRepo.Setup(r => r.GetActiveByPhoneAsync("0901234567", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _customerService.CreateAsync(request));
        Assert.Contains("0901234567", ex.Message);
    }

    #endregion

    #region GetById & Lookup Tests

    [Fact]
    public async Task GetByIdAsync_ExistingCustomer_ShouldReturnCustomerResponseDto()
    {
        var customer = new Customer
        {
            CustomerId = 1,
            LoyaltyPoints = 25,
            Status = 1,
            Gender = 1,
            Address = "TP. Hồ Chí Minh",
            User = new User
            {
                FullName = "Nguyễn Văn An",
                PhoneNumber = "0901234567",
                Email = "an@example.com",
                DateOfBirth = new DateTime(2000, 5, 20)
            }
        };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _customerService.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.CustomerId);
        Assert.Equal("Nguyễn Văn An", result.FullName);
        Assert.Equal(25, result.LoyaltyPoints);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentCustomer_ShouldReturnNull()
    {
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _customerService.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task LookupByPhoneAsync_Found_ShouldReturnLookupDto()
    {
        var customer = new Customer
        {
            CustomerId = 1,
            LoyaltyPoints = 25,
            Status = 1,
            User = new User
            {
                FullName = "Nguyễn Văn An",
                PhoneNumber = "0901234567"
            }
        };

        _mockCustomerRepo.Setup(r => r.GetByPhoneAsync("0901234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _customerService.LookupByPhoneAsync("0901234567");

        Assert.NotNull(result);
        Assert.Equal(1, result.CustomerId);
        Assert.Equal("Nguyễn Văn An", result.FullName);
        Assert.Equal("0901234567", result.Phone);
        Assert.Equal(25, result.LoyaltyPoints);
    }

    [Fact]
    public async Task LookupByPhoneAsync_NotFound_ShouldReturnNull()
    {
        _mockCustomerRepo.Setup(r => r.GetByPhoneAsync("0999999999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _customerService.LookupByPhoneAsync("0999999999");

        Assert.Null(result);
    }

    #endregion

    #region Update & Delete Tests

    [Fact]
    public async Task UpdateAsync_ValidData_ShouldUpdateCustomer()
    {
        var existingCustomer = new Customer
        {
            CustomerId = 1,
            LoyaltyPoints = 25,
            Status = 1,
            User = new User
            {
                FullName = "Nguyễn Văn An",
                PhoneNumber = "0901234567",
                Email = "an@example.com"
            }
        };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockCustomerRepo.Setup(r => r.GetActiveByPhoneAsync("0909876543", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var updateRequest = new UpdateCustomerRequest
        {
            FullName = "Nguyễn Văn An Mới",
            Phone = "0909876543",
            Email = "an.new@example.com",
            Address = "Hà Nội"
        };

        var result = await _customerService.UpdateAsync(1, updateRequest);

        Assert.NotNull(result);
        Assert.Equal("Nguyễn Văn An Mới", result.FullName);
        Assert.Equal("0909876543", result.Phone);
        Assert.Equal("Hà Nội", result.Address);
        Assert.Equal(25, result.LoyaltyPoints); // Immutable points

        _mockCustomerRepo.Verify(r => r.Update(existingCustomer), Times.Once);
        _mockCustomerRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_SetsStatusToInactive()
    {
        var existingCustomer = new Customer
        {
            CustomerId = 1,
            Status = 1,
            User = new User { Status = UserStatus.Active }
        };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var success = await _customerService.DeleteAsync(1);

        Assert.True(success);
        Assert.Equal(2, existingCustomer.Status); // INACTIVE
        Assert.Equal(UserStatus.Locked, existingCustomer.User.Status);
        _mockCustomerRepo.Verify(r => r.Update(existingCustomer), Times.Once);
        _mockCustomerRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Loyalty & Order & Voucher History Tests

    [Fact]
    public async Task GetLoyaltyPointsAsync_ReturnsCurrentPoints()
    {
        var customer = new Customer
        {
            CustomerId = 1,
            LoyaltyPoints = 50
        };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _customerService.GetLoyaltyPointsAsync(1);

        Assert.Equal(1, result.CustomerId);
        Assert.Equal(50, result.LoyaltyPoints);
    }

    [Fact]
    public async Task GetLoyaltyHistoryAsync_ReturnsPointsHistoryList()
    {
        var customer = new Customer { CustomerId = 1, LoyaltyPoints = 25 };
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var pointHistories = new List<PointHistory>
        {
            new PointHistory { PointHistoryId = 100, CustomerId = 1, OrderId = 105, PointChange = 25, Type = 1, CreatedAt = DateTime.UtcNow }
        };

        _mockCustomerRepo.Setup(r => r.GetPointHistoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pointHistories);

        var result = await _customerService.GetLoyaltyHistoryAsync(1);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(100, result.Items[0].PointHistoryId);
        Assert.Equal(25, result.Items[0].PointChange);
        Assert.Equal(1, result.Items[0].Type);
    }

    [Fact]
    public async Task GetOrderHistoryAsync_ReturnsCustomerOrders()
    {
        var customer = new Customer { CustomerId = 1 };
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var orders = new List<Order>
        {
            new Order
            {
                OrderId = 105,
                CustomerId = 1,
                BranchId = 1,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 300000m,
                DiscountAmount = 50000m,
                FinalAmount = 250000m,
                Status = OrderStatus.Completed
            }
        };

        _mockCustomerRepo.Setup(r => r.GetOrdersAsync(1, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 1));

        var result = await _customerService.GetOrderHistoryAsync(1, null, null, null, null, 1, 10);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(105, result.Items.First().OrderId);
        Assert.Equal(250000m, result.Items.First().FinalAmount);
    }

    [Fact]
    public async Task GetVouchersAsync_ReturnsVouchers()
    {
        var customer = new Customer { CustomerId = 1 };
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var vouchers = new List<Voucher>
        {
            new Voucher
            {
                VoucherId = 3,
                CustomerId = 1,
                Code = "CHAOMUNG2026",
                DiscountAmount = 50000m,
                IsUsed = false,
                ExpiryDate = DateTime.UtcNow.AddMonths(1)
            }
        };

        _mockCustomerRepo.Setup(r => r.GetVouchersAsync(1, "available", It.IsAny<CancellationToken>()))
            .ReturnsAsync(vouchers);

        var result = await _customerService.GetVouchersAsync(1, "available");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("CHAOMUNG2026", result[0].Code);
        Assert.False(result[0].IsUsed);
    }

    [Fact]
    public async Task AddPointsAsync_ValidPoints_AddsPointsAndRecordsHistory()
    {
        var customer = new Customer
        {
            CustomerId = 1,
            LoyaltyPoints = 100,
            MembershipTier = 1,
            User = new User { FullName = "Nguyễn Văn An" }
        };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _customerService.AddPointsAsync(1, new AddPointsRequest { PointsToAdd = 50 });

        Assert.Equal(150, customer.LoyaltyPoints);
        Assert.Equal(150, result.LoyaltyPoints);

        _mockCustomerRepo.Verify(r => r.AddPointHistoryAsync(
            It.Is<PointHistory>(p => p.CustomerId == 1 && p.PointChange == 50 && p.Type == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockCustomerRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RedeemVoucherAsync_SufficientPoints_DeductsPointsAndCreatesVoucher()
    {
        var customer = new Customer
        {
            CustomerId = 1,
            LoyaltyPoints = 1000,
            MembershipTier = 2,
            User = new User { FullName = "Nguyễn Văn An" }
        };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var response = await _customerService.RedeemVoucherAsync(1, new RedeemVoucherRequest { PointsToRedeem = 1000 });

        Assert.True(response.IsSuccess);
        Assert.Equal(0, customer.LoyaltyPoints); // 1000 - 1000 = 0
        Assert.Equal(50000m, response.VoucherValue);

        _mockCustomerRepo.Verify(r => r.AddPointHistoryAsync(
            It.Is<PointHistory>(p => p.CustomerId == 1 && p.PointChange == -1000 && p.Type == 3),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockCustomerRepo.Verify(r => r.AddVoucherAsync(
            It.Is<Voucher>(v => v.CustomerId == 1 && v.DiscountAmount == 50000m && !v.IsUsed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
