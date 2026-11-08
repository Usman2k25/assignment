namespace CampusServices.Infrastructure.Auth;

// Matches the "Jwt" section of appsettings.json.
// The secret Key is NOT in appsettings.json - it comes from user-secrets or an environment variable.
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}
