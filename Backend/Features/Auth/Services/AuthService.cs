using SmartSupermarket.Backend.Features.Auth.DTOs;

namespace SmartSupermarket.Backend.Features.Auth.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    public Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // TODO: Implement Auth Logic (Password Verification & Jwt Token Generation)
        throw new NotImplementedException();
    }
}
