using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Employees.DTOs;
using SmartSupermarket.Backend.Features.Employees.Services;
using SmartSupermarket.Backend.Infrastructure.Persistence;
using Xunit;

namespace SmartSupermarket.Backend.Tests;

public class ShiftSessionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ShiftSessionService _service;

    public ShiftSessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ShiftSessionTestDb_{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(options);

        // Seed users
        _context.Users.AddRange(
            new User
            {
                UserId = 1,
                Username = "staff1",
                FullName = "Nguyễn Văn Thu Ngân",
                Role = UserRole.Staff,
                Status = UserStatus.Active,
                ManagerPin = "123456"
            },
            new User
            {
                UserId = 2,
                Username = "manager1",
                FullName = "Trần Thị Quản Lý",
                Role = UserRole.Manager,
                Status = UserStatus.Active,
                ManagerPin = "888888"
            }
        );

        _context.Shifts.Add(new Shift
        {
            ShiftId = 1,
            Code = "S1",
            Name = "Ca Sáng (06:00 - 14:00)",
            StartTime = TimeSpan.FromHours(6),
            EndTime = TimeSpan.FromHours(14),
            Status = "Active"
        });

        _context.SaveChanges();

        _service = new ShiftSessionService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task OpenSessionAsync_ShouldCreateNewOpenSession_AndSetUserOnline()
    {
        // Act
        var result = await _service.OpenSessionAsync(new OpenShiftSessionRequest
        {
            EmployeeId = 1,
            ShiftId = 1,
            BranchId = 1,
            WorkstationName = "POS-01",
            InitialCash = 2000000m
        });

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SessionId > 0);
        Assert.Equal("Open", result.Status);
        Assert.Equal(2000000m, result.InitialCash);
        Assert.Equal("POS-01", result.WorkstationName);

        var user = await _context.Users.FindAsync(1);
        Assert.NotNull(user);
        Assert.True(user.IsOnline);
        Assert.Equal("POS-01", user.CurrentMachineName);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_ShouldReturnActiveSession_WithCalculatedCashSales()
    {
        // Arrange
        var openResult = await _service.OpenSessionAsync(new OpenShiftSessionRequest
        {
            EmployeeId = 1,
            ShiftId = 1,
            BranchId = 1,
            WorkstationName = "POS-01",
            InitialCash = 1000000m
        });

        // Seed 1 order under this shift session with Cash payment
        var order = new Order
        {
            OrderId = 101,
            EmployeeId = 1,
            BranchId = 1,
            ShiftSessionId = openResult.SessionId,
            FinalAmount = 250000m,
            Status = OrderStatus.Completed,
            Payments = new List<Payment>
            {
                new Payment { PaymentMethod = PaymentMethod.Cash, AmountPaid = 250000m }
            }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var current = await _service.GetCurrentSessionAsync(1, 1);

        // Assert
        Assert.NotNull(current);
        Assert.Equal(1, current.TotalOrdersCount);
        Assert.Equal(250000m, current.CashSalesAmount);
        Assert.Equal(250000m, current.TotalRevenue);
        // Expected Cash = Initial (1,000,000) + Cash Sales (250,000) = 1,250,000
        Assert.Equal(1250000m, current.ExpectedCash);
    }

    [Fact]
    public async Task CloseSessionAsync_WithDiscrepancyAndNoReason_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var openResult = await _service.OpenSessionAsync(new OpenShiftSessionRequest
        {
            EmployeeId = 1,
            ShiftId = 1,
            BranchId = 1,
            WorkstationName = "POS-01",
            InitialCash = 1000000m
        });

        // Act & Assert: Expected is 1,000,000, but Actual is 900,000 (missing 100k) with NO reason
        var req = new CloseShiftSessionRequest
        {
            SessionId = openResult.SessionId,
            ActualCash = 900000m,
            DiscrepancyReason = null // Thiếu giải trình
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CloseSessionAsync(req));
        Assert.Contains("Quy định bắt buộc Thu ngân phải nhập lý do giải trình", ex.Message);
    }

    [Fact]
    public async Task CloseSessionAsync_WithDiscrepancyAndReason_ShouldSucceedAndMarkClosed()
    {
        // Arrange
        var openResult = await _service.OpenSessionAsync(new OpenShiftSessionRequest
        {
            EmployeeId = 1,
            ShiftId = 1,
            BranchId = 1,
            WorkstationName = "POS-01",
            InitialCash = 1000000m
        });

        // Act: Actual is 1,050,000 (Surplus 50k) with explanation and Manager PIN
        var req = new CloseShiftSessionRequest
        {
            SessionId = openResult.SessionId,
            ActualCash = 1050000m,
            DiscrepancyReason = "Khách hàng quên lấy tiền thừa",
            ManagerPin = "888888",
            ManagerNote = "Đã đối chiếu camera"
        };

        var closed = await _service.CloseSessionAsync(req);

        // Assert
        Assert.Equal("Closed", closed.Status);
        Assert.Equal(1050000m, closed.ActualCash);
        Assert.Equal(50000m, closed.DifferenceAmount);
        Assert.Equal("Khách hàng quên lấy tiền thừa", closed.DiscrepancyReason);

        var user = await _context.Users.FindAsync(1);
        Assert.NotNull(user);
        Assert.False(user.IsOnline); // Offline after shift closure
    }

    [Fact]
    public async Task VerifyManagerPinAsync_WithCorrectPin_ShouldReturnTrue()
    {
        bool result = await _service.VerifyManagerPinAsync("888888");
        Assert.True(result);

        bool defaultPin = await _service.VerifyManagerPinAsync("123456");
        Assert.True(defaultPin);

        bool wrongPin = await _service.VerifyManagerPinAsync("999999");
        Assert.False(wrongPin);
    }

    [Fact]
    public async Task GetOnlineWorkstationsAsync_ShouldListWorkstationsAndSales()
    {
        // Arrange
        await _service.OpenSessionAsync(new OpenShiftSessionRequest
        {
            EmployeeId = 1,
            ShiftId = 1,
            BranchId = 1,
            WorkstationName = "POS-QUAY-01",
            InitialCash = 2000000m
        });

        // Act
        var workstations = await _service.GetOnlineWorkstationsAsync(1);

        // Assert
        Assert.NotNull(workstations);
        Assert.NotEmpty(workstations);

        var staffStation = workstations.FirstOrDefault(w => w.UserId == 1);
        Assert.NotNull(staffStation);
        Assert.True(staffStation.IsOnline);
        Assert.Equal("POS-QUAY-01", staffStation.WorkstationName);
    }
}
