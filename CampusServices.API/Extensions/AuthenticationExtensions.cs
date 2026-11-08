using System.Text;
using CampusServices.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CampusServices.API.Extensions;

public static class AuthenticationExtensions
{
    // Tells ASP.NET Core how to check the JWT sent in the "Authorization: Bearer ..." header.
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt").Get<JwtSettings>();

        // Fail early with a clear message instead of a confusing error later.
        if (jwt == null || string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 characters. Set it with user-secrets (see README.md).");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,          // reject expired tokens
                    ClockSkew = TimeSpan.Zero         // no extra grace time after expiry
                };
            });

        services.AddAuthorization();

        return services;
    }
}
