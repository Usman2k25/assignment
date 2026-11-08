using CampusServices.Application.DTOs;

namespace CampusServices.Application.Interfaces;

// Register, login and token handling. Implemented in Infrastructure because it uses ASP.NET Core Identity.
public interface IAuthService
{
    // Always creates a Student. Throws DomainException if the email is taken or the password is too weak.
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

    // Returns null when the email or password is wrong.
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);

    // Swaps a valid refresh token for a NEW access token + NEW refresh token. Returns null if invalid.
    Task<AuthResponseDto?> RefreshAsync(string refreshToken);

    // Revokes the refresh token so it can never be used again.
    Task LogoutAsync(string refreshToken);
}
