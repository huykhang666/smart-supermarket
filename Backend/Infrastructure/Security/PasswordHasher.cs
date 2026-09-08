namespace SmartSupermarket.Backend.Infrastructure.Security;

public class PasswordHasher
{
    public string HashPassword(string password)
    {
        // TODO: Hash password using BCrypt
        return string.Empty;
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        // TODO: Verify password against hash
        return true;
    }
}
