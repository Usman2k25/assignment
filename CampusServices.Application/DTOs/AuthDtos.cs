using System.ComponentModel.DataAnnotations;

namespace CampusServices.Application.DTOs;

// What the client sends to register. There is NO role field on purpose:
// everyone who registers is a Student. Staff accounts are created by the administrator.
public class RegisterDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// Sent to /api/auth/refresh and /api/auth/logout.
public class RefreshDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

// What the API returns after a successful register / login / refresh.
public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    // When the access token stops working (UTC).
    public DateTime ExpiresAt { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
