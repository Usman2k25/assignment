using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CampusServices.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CampusServices.Infrastructure.Auth;

// Creates the two kinds of tokens. It knows nothing about the database.
public class TokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    // ACCESS TOKEN: a signed JWT that proves who the user is. Short life (30 minutes).
    // The claims inside are what the API reads on every request, including the role.
    public string CreateAccessToken(ApplicationUser user, string role, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // REFRESH TOKEN: just a long random text. Not a JWT - it only has meaning because we store it.
    public string CreateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    // We save only the hash (SHA-256) of the refresh token in the database.
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
