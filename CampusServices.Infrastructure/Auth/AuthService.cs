using CampusServices.Application.DTOs;
using CampusServices.Application.Interfaces;
using CampusServices.Domain;
using CampusServices.Domain.Entities;
using CampusServices.Domain.Exceptions;
using CampusServices.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampusServices.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly JwtSettings _settings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        TokenService tokenService,
        IOptions<JwtSettings> options)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _settings = options.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var user = new ApplicationUser();
        user.UserName = dto.Email.Trim();
        user.Email = dto.Email.Trim();
        user.FullName = dto.FullName.Trim();

        // Identity hashes the password for us. We never store the plain password.
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var messages = new List<string>();
            foreach (var error in result.Errors)
            {
                messages.Add(error.Description);
            }
            throw new DomainException(string.Join(" ", messages));
        }

        // Anyone who registers is a Student. There is no way to ask for the Staff role here.
        await _userManager.AddToRoleAsync(user, Roles.Student);

        return await CreateResponseAsync(user, Roles.Student);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user == null)
        {
            return null;
        }

        // After several wrong passwords Identity locks the account for a few minutes.
        if (await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        var passwordIsCorrect = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordIsCorrect)
        {
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var role = await GetRoleAsync(user);
        return await CreateResponseAsync(user, role);
    }

    public async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
    {
        var saved = await FindRefreshTokenAsync(refreshToken);
        if (saved == null || !saved.IsActive)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(saved.UserId);
        if (user == null)
        {
            return null;
        }

        // ROTATION: the old refresh token is used up. CreateResponseAsync below
        // saves this change together with the brand-new token.
        saved.RevokedAt = DateTime.UtcNow;

        var role = await GetRoleAsync(user);
        return await CreateResponseAsync(user, role);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var saved = await FindRefreshTokenAsync(refreshToken);
        if (saved == null || saved.RevokedAt != null)
        {
            return;
        }

        // REVOCATION: after this the token can never be used again.
        saved.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ---------- private helpers ----------

    private async Task<string> GetRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.Staff))
        {
            return Roles.Staff;
        }
        return Roles.Student;
    }

    private async Task<RefreshToken?> FindRefreshTokenAsync(string refreshToken)
    {
        var hash = TokenService.Hash(refreshToken);
        return await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
    }

    // Builds the answer for the client: a new access token and a new refresh token.
    private async Task<AuthResponseDto> CreateResponseAsync(ApplicationUser user, string role)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);
        var accessToken = _tokenService.CreateAccessToken(user, role, expiresAt);

        var refreshToken = _tokenService.CreateRefreshToken();
        var saved = new RefreshToken();
        saved.UserId = user.Id;
        saved.TokenHash = TokenService.Hash(refreshToken);
        saved.CreatedAt = DateTime.UtcNow;
        saved.ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays);
        _db.RefreshTokens.Add(saved);
        await _db.SaveChangesAsync();

        var response = new AuthResponseDto();
        response.AccessToken = accessToken;
        response.RefreshToken = refreshToken;
        response.ExpiresAt = expiresAt;
        response.UserId = user.Id;
        response.FullName = user.FullName;
        response.Email = user.Email ?? string.Empty;
        response.Role = role;
        return response;
    }
}
