namespace SmartSupermarket.Backend.Infrastructure.Security;

public class JwtTokenService
{
    public string GenerateToken(int userId, string username, string role)
    {
        // TODO: Generate JWT Bearer token with Claims
        return string.Empty;
    }
}

public class PasswordHasher
{
    public string HashPassword(string password)
    {
        // TODO: Hash password using BCrypt
        return string.Empty;
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        // TODO: Verify password against BCrypt hash
        return true;
    }
}
