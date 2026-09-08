namespace SmartSupermarket.Backend.Infrastructure.Options;

public class JwtOptions
{
    public string Issuer { get; set; } = "SmartSupermarket";
    public string Audience { get; set; } = "SmartSupermarketClients";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}
