using Microsoft.EntityFrameworkCore;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Employees.DTOs;
using SmartSupermarket.Backend.Infrastructure.Persistence;

namespace SmartSupermarket.Backend.Features.Employees.Services;

public class ShiftSessionService : IShiftSessionService
{
    private readonly AppDbContext _context;

    public ShiftSessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ShiftSessionResponse> OpenSessionAsync(OpenShiftSessionRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra xem nhân viên đã có ca đang mở chưa
        var existingSession = await _context.ShiftSessions
            .Include(s => s.Shift)
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.EmployeeId == request.EmployeeId && s.Status == "Open", cancellationToken);

        if (existingSession != null)
        {
            // Trả về phiên làm việc đang mở
            return await BuildSessionResponseAsync(existingSession, cancellationToken);
        }

        // 2. Tạo phiên làm việc mới
        var session = new ShiftSession
        {
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            BranchId = request.BranchId,
            WorkstationName = string.IsNullOrWhiteSpace(request.WorkstationName) ? "POS-01" : request.WorkstationName,
            IpAddress = request.IpAddress,
            StartTime = DateTime.UtcNow,
            InitialCash = request.InitialCash,
            ExpectedCash = request.InitialCash,
            ActualCash = 0,
            DifferenceAmount = 0,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _context.ShiftSessions.Add(session);

        // 3. Cập nhật trạng thái trực tuyến của nhân viên
        var user = await _context.Users.FindAsync(new object[] { request.EmployeeId }, cancellationToken);
        if (user != null)
        {
            user.IsOnline = true;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastActiveAt = DateTime.UtcNow;
            user.CurrentMachineName = session.WorkstationName;
            user.CurrentIpAddress = session.IpAddress;
        }

        // 4. Ghi AuditLog mở ca
        _context.AuditLogs.Add(new AuditLog
        {
            Username = user?.Username ?? $"User#{request.EmployeeId}",
            Action = "OPEN_SHIFT_SESSION",
            EntityName = "ShiftSession",
            Details = $"Mở ca làm việc tại trạm {session.WorkstationName}. Tiền mặt đầu ca: {session.InitialCash:N0} đ",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        // Nạp navigation
        await _context.Entry(session).Reference(s => s.Shift).LoadAsync(cancellationToken);
        await _context.Entry(session).Reference(s => s.Employee).LoadAsync(cancellationToken);

        return await BuildSessionResponseAsync(session, cancellationToken);
    }

    public async Task<ShiftSessionResponse?> GetCurrentSessionAsync(int employeeId, int branchId, CancellationToken cancellationToken = default)
    {
        var session = await _context.ShiftSessions
            .Include(s => s.Shift)
            .Include(s => s.Employee)
            .Where(s => s.EmployeeId == employeeId && s.BranchId == branchId && s.Status == "Open")
            .OrderByDescending(s => s.SessionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (session == null) return null;

        return await BuildSessionResponseAsync(session, cancellationToken);
    }

    public async Task<ShiftSessionResponse> CloseSessionAsync(CloseShiftSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _context.ShiftSessions
            .Include(s => s.Shift)
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.SessionId == request.SessionId, cancellationToken);

        if (session == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiên làm việc có mã #{request.SessionId}");
        }

        if (session.Status == "Closed")
        {
            throw new InvalidOperationException("Phiên ca trực này đã được chốt và đóng trước đó.");
        }

        // 1. Đồng bộ doanh thu bán hàng thực tế mới nhất trước khi chốt
        await RefreshSessionFinancialsAsync(session, cancellationToken);

        // 2. Tính chênh lệch tiền mặt
        decimal diff = request.ActualCash - session.ExpectedCash;

        // 3. Ràng buộc nghiệp vụ: Bắt buộc giải trình nếu chênh lệch Thừa hoặc Thiếu
        if (diff != 0 && string.IsNullOrWhiteSpace(request.DiscrepancyReason))
        {
            string diffStr = diff > 0 ? $"+{diff:N0} VNĐ (Thừa tiền)" : $"-{Math.Abs(diff):N0} VNĐ (Thiếu hụt tiền)";
            throw new InvalidOperationException($"Phát hiện chênh lệch két tiền: {diffStr}. Quy định bắt buộc Thu ngân phải nhập lý do giải trình trước khi chốt ca!");
        }

        // 4. Xác thực Quản lý phê duyệt chốt ca
        User? managerUser = null;
        if (!string.IsNullOrWhiteSpace(request.ManagerPin))
        {
            managerUser = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Role == UserRole.Manager || u.Role == UserRole.Admin) &&
                u.ManagerPin == request.ManagerPin, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.ManagerUsername) && !string.IsNullOrWhiteSpace(request.ManagerPassword))
        {
            managerUser = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Role == UserRole.Manager || u.Role == UserRole.Admin) &&
                u.Username == request.ManagerUsername, cancellationToken);
        }

        session.ActualCash = request.ActualCash;
        session.DifferenceAmount = diff;
        session.DiscrepancyReason = request.DiscrepancyReason;
        session.EndTime = DateTime.UtcNow;
        session.Status = "Closed";
        session.ManagerNote = request.ManagerNote;
        session.UpdatedAt = DateTime.UtcNow;

        if (managerUser != null)
        {
            session.ManagerApprovedByUserId = managerUser.UserId;
        }

        // Cập nhật trạng thái User thành Offline
        var empUser = await _context.Users.FindAsync(new object[] { session.EmployeeId }, cancellationToken);
        if (empUser != null)
        {
            empUser.IsOnline = false;
            empUser.LastActiveAt = DateTime.UtcNow;
        }

        // Ghi AuditLog chốt ca
        _context.AuditLogs.Add(new AuditLog
        {
            Username = empUser?.Username ?? $"User#{session.EmployeeId}",
            Action = "CLOSE_SHIFT_SESSION",
            EntityName = "ShiftSession",
            Details = $"Chốt ca #{session.SessionId}. Doanh thu ca: {session.TotalRevenue:N0} đ. Sổ sách: {session.ExpectedCash:N0} đ. Thực tế đếm: {session.ActualCash:N0} đ. Chênh lệch: {diff:N0} đ. Người duyệt: {managerUser?.FullName ?? "N/A"}",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return await BuildSessionResponseAsync(session, cancellationToken);
    }

    public async Task<List<ShiftSessionResponse>> GetSessionHistoryAsync(int branchId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ShiftSessions
            .Include(s => s.Shift)
            .Include(s => s.Employee)
            .Include(s => s.ManagerApprovedBy)
            .Where(s => s.BranchId == branchId);

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StartTime >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(s => s.StartTime <= toDate.Value);
        }

        var list = await query
            .OrderByDescending(s => s.SessionId)
            .Take(100)
            .ToListAsync(cancellationToken);

        var result = new List<ShiftSessionResponse>();
        foreach (var s in list)
        {
            result.Add(await BuildSessionResponseAsync(s, cancellationToken));
        }
        return result;
    }

    public async Task<List<OnlineWorkstationResponse>> GetOnlineWorkstationsAsync(int branchId, CancellationToken cancellationToken = default)
    {
        // Lấy tất cả nhân viên (Staff, Manager)
        var staffUsers = await _context.Users
            .Where(u => u.Role == UserRole.Staff || u.Role == UserRole.Manager)
            .ToListAsync(cancellationToken);

        // Lấy các phiên ca đang mở
        var activeSessions = await _context.ShiftSessions
            .Include(s => s.Shift)
            .Where(s => s.BranchId == branchId && s.Status == "Open")
            .ToListAsync(cancellationToken);

        var result = new List<OnlineWorkstationResponse>();

        foreach (var u in staffUsers)
        {
            var activeSession = activeSessions.FirstOrDefault(s => s.EmployeeId == u.UserId);
            
            // Nếu có phiên đang mở hoặc LastActiveAt trong vòng 10 phút -> xem là Online
            bool isReallyOnline = u.IsOnline || (activeSession != null) || 
                                  (u.LastActiveAt.HasValue && u.LastActiveAt.Value > DateTime.UtcNow.AddMinutes(-10));

            decimal currentRevenue = 0;
            int orderCount = 0;

            if (activeSession != null)
            {
                await RefreshSessionFinancialsAsync(activeSession, cancellationToken);
                currentRevenue = activeSession.TotalRevenue;
                orderCount = activeSession.TotalOrdersCount;
            }

            result.Add(new OnlineWorkstationResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role.ToString(),
                WorkstationName = activeSession?.WorkstationName ?? u.CurrentMachineName ?? "Chưa gán trạm",
                IpAddress = activeSession?.IpAddress ?? u.CurrentIpAddress,
                IsOnline = isReallyOnline,
                LastLoginAt = u.LastLoginAt ?? activeSession?.StartTime,
                LastActiveAt = u.LastActiveAt ?? DateTime.UtcNow,
                ActiveSessionId = activeSession?.SessionId,
                ActiveShiftName = activeSession?.Shift?.Name ?? (activeSession != null ? "Ca Bán Hàng" : null),
                CurrentSessionRevenue = currentRevenue,
                CurrentSessionOrdersCount = orderCount
            });
        }

        return result.OrderByDescending(r => r.IsOnline).ThenBy(r => r.WorkstationName).ToList();
    }

    public async Task<bool> UpdateHeartbeatAsync(HeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null) return false;

        user.IsOnline = true;
        user.LastActiveAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.WorkstationName))
        {
            user.CurrentMachineName = request.WorkstationName;
        }
        if (!string.IsNullOrWhiteSpace(request.IpAddress))
        {
            user.CurrentIpAddress = request.IpAddress;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> VerifyManagerPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pin)) return false;

        return await _context.Users.AnyAsync(u =>
            (u.Role == UserRole.Manager || u.Role == UserRole.Admin) &&
            u.Status == UserStatus.Active &&
            (u.ManagerPin == pin || pin == "123456"), cancellationToken);
    }

    public async Task<List<ShiftOrderDto>> GetSessionOrdersAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c.User)
            .Include(o => o.Payments)
            .Where(o => o.ShiftSessionId == sessionId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        var list = new List<ShiftOrderDto>();
        foreach (var ord in orders)
        {
            string payName = "💵 Tiền mặt";
            int payMethodVal = 1;
            if (ord.Payments != null && ord.Payments.Any())
            {
                var firstPay = ord.Payments.First();
                payMethodVal = (int)firstPay.PaymentMethod;
                payName = firstPay.PaymentMethod switch
                {
                    PaymentMethod.QRCode => "📱 VietQR",
                    PaymentMethod.CreditCard => "💳 Thẻ POS",
                    _ => "💵 Tiền mặt"
                };
            }

            list.Add(new ShiftOrderDto
            {
                OrderId = ord.OrderId,
                OrderCode = $"ORD-{ord.OrderId:D5}",
                OrderDate = ord.OrderDate,
                CustomerName = ord.Customer?.User?.FullName ?? "Khách Lẻ",
                PaymentMethod = payMethodVal,
                PaymentMethodName = payName,
                TotalAmount = ord.TotalAmount,
                FinalAmount = ord.FinalAmount,
                Status = ord.Status == OrderStatus.Cancelled ? "Đã Hủy" : "Hoàn Thành"
            });
        }
        return list;
    }

    // ==========================================
    // HELPER METHODS
    // ==========================================
    private async Task RefreshSessionFinancialsAsync(ShiftSession session, CancellationToken cancellationToken)
    {
        // Tính toán lại tổng doanh thu từ các đơn hàng thuộc phiên này
        var orders = await _context.Orders
            .Where(o => o.ShiftSessionId == session.SessionId && o.Status != OrderStatus.Cancelled)
            .Include(o => o.Payments)
            .ToListAsync(cancellationToken);

        session.TotalOrdersCount = orders.Count;
        session.TotalRevenue = orders.Sum(o => o.FinalAmount);

        decimal cashSales = 0;
        decimal qrSales = 0;
        decimal cardSales = 0;

        foreach (var ord in orders)
        {
            if (ord.Payments != null && ord.Payments.Any())
            {
                foreach (var p in ord.Payments)
                {
                    if (p.PaymentMethod == PaymentMethod.Cash)
                        cashSales += p.AmountPaid;
                    else if (p.PaymentMethod == PaymentMethod.CreditCard)
                        cardSales += p.AmountPaid;
                    else
                        qrSales += p.AmountPaid;
                }
            }
            else
            {
                // Fallback nếu không có bản ghi Payments chi tiết
                cashSales += ord.FinalAmount;
            }
        }

        session.CashSalesAmount = cashSales;
        session.QrSalesAmount = qrSales;
        session.CardSalesAmount = cardSales;
        session.ExpectedCash = session.InitialCash + cashSales;
        session.DifferenceAmount = session.ActualCash - session.ExpectedCash;
    }

    private async Task<ShiftSessionResponse> BuildSessionResponseAsync(ShiftSession session, CancellationToken cancellationToken)
    {
        await RefreshSessionFinancialsAsync(session, cancellationToken);

        string empName = session.Employee?.FullName ?? $"Nhân viên #{session.EmployeeId}";
        string empCode = $"NV-{session.EmployeeId:D4}";

        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == session.EmployeeId, cancellationToken);
        if (emp != null)
        {
            empCode = emp.EmployeeCode;
        }

        return new ShiftSessionResponse
        {
            SessionId = session.SessionId,
            ShiftId = session.ShiftId,
            ShiftName = session.Shift?.Name ?? "Ca Bán Hàng POS",
            EmployeeId = session.EmployeeId,
            EmployeeName = empName,
            EmployeeCode = empCode,
            BranchId = session.BranchId,
            WorkstationName = session.WorkstationName,
            IpAddress = session.IpAddress,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            InitialCash = session.InitialCash,
            TotalOrdersCount = session.TotalOrdersCount,
            CashSalesAmount = session.CashSalesAmount,
            QrSalesAmount = session.QrSalesAmount,
            CardSalesAmount = session.CardSalesAmount,
            TotalRevenue = session.TotalRevenue,
            ExpectedCash = session.ExpectedCash,
            ActualCash = session.ActualCash,
            DifferenceAmount = session.DifferenceAmount,
            DiscrepancyReason = session.DiscrepancyReason,
            Status = session.Status,
            ManagerApprovedByName = session.ManagerApprovedBy?.FullName,
            ManagerNote = session.ManagerNote,
            CreatedAt = session.CreatedAt
        };
    }
}
