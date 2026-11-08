using CampusServices.Application.DTOs;
using CampusServices.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CampusServices.API.Controllers;

// Anyone can call these endpoints (no [Authorize]) because the user is not logged in yet.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST api/auth/register  -> always creates a Student
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return Ok(result);
    }

    // POST api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid email or password."
            });
        }

        return Ok(result);
    }

    // POST api/auth/refresh  -> exchange a refresh token for a new access token + new refresh token
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshDto dto)
    {
        var result = await _authService.RefreshAsync(dto.RefreshToken);
        if (result == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "The refresh token is invalid, expired or already used."
            });
        }

        return Ok(result);
    }

    // POST api/auth/logout  -> revokes the refresh token
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshDto dto)
    {
        await _authService.LogoutAsync(dto.RefreshToken);
        return NoContent();
    }
}
