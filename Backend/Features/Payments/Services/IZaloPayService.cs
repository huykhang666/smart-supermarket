using SmartSupermarket.Backend.Features.Payments.DTOs;

namespace SmartSupermarket.Backend.Features.Payments.Services;

public interface IZaloPayService
{
    Task<(bool isSuccess, string? orderUrl, string? qrCode, string? error)> CreateOrderAsync(string appTransId, decimal amount, string description);
    bool VerifyCallback(string data, string mac);
    Task<(int returnCode, string returnMessage, string? zpTransId)> QueryOrderStatusAsync(string appTransId);
}
