using CampusServices.Application.Interfaces;
using CampusServices.Domain.Entities;
using CampusServices.Infrastructure.Auth;
using CampusServices.Infrastructure.Data;
using CampusServices.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusServices.Infrastructure;

// One method that registers everything Infrastructure provides.
// The API calls it once in Program.cs: builder.Services.AddInfrastructure(...)
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. Set it with user-secrets (see README.md).");
        }

        // Database: EF Core with PostgreSQL.
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // ASP.NET Core Identity: users, roles and password hashing.
        // (AddIdentityCore is used because we log in with JWT, not with cookies.)
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        // JWT settings + token creation + login/register logic.
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Repositories: each interface (from Application) is connected to its EF Core class.
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
