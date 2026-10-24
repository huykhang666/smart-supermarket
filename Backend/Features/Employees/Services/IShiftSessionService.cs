using SmartSupermarket.Backend.Features.Employees.DTOs;

namespace SmartSupermarket.Backend.Features.Employees.Services;

public interface IShiftSessionService
{
    Task<ShiftSessionResponse> OpenSessionAsync(OpenShiftSessionRequest request, CancellationToken cancellationToken = default);
    Task<ShiftSessionResponse?> GetCurrentSessionAsync(int employeeId, int branchId, CancellationToken cancellationToken = default);
    Task<ShiftSessionResponse> CloseSessionAsync(CloseShiftSessionRequest request, CancellationToken cancellationToken = default);
    Task<List<ShiftSessionResponse>> GetSessionHistoryAsync(int branchId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<List<OnlineWorkstationResponse>> GetOnlineWorkstationsAsync(int branchId, CancellationToken cancellationToken = default);
    Task<bool> UpdateHeartbeatAsync(HeartbeatRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyManagerPinAsync(string pin, CancellationToken cancellationToken = default);
    Task<List<ShiftOrderDto>> GetSessionOrdersAsync(int sessionId, CancellationToken cancellationToken = default);
}
