using CampusServices.Application.DTOs;

namespace CampusServices.MAUI.Services;

// What the Login / Register pages and the Logout button use.
// It calls the API and then saves (or clears) the session.
public class AuthService
{
    private readonly ApiClient _api;
    private readonly SessionStore _sessionStore;

    public AuthService(ApiClient api, SessionStore sessionStore)
    {
        _api = api;
        _sessionStore = sessionStore;
    }

    public async Task<AuthResponseDto?> GetSessionAsync()
    {
        return await _sessionStore.GetAsync();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var session = await _api.LoginAsync(dto);
        await _sessionStore.SaveAsync(session);
        return session;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var session = await _api.RegisterAsync(dto);
        await _sessionStore.SaveAsync(session);
        return session;
    }

    public async Task LogoutAsync()
    {
        var session = await _sessionStore.GetAsync();
        if (session != null)
        {
            try
            {
                // Tell the API to revoke the refresh token.
                await _api.LogoutAsync(session.RefreshToken);
            }
            catch (ApiException)
            {
                // Even if the API cannot be reached we still log out on this device.
            }
        }

        _sessionStore.Clear();
    }
}
