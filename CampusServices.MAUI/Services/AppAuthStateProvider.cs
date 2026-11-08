using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace CampusServices.MAUI.Services;

// Tells Blazor "who is logged in and what is their role".
// <AuthorizeView> and [Authorize] ask this class.
// NOTE: this only controls what the screen shows. The API checks the role again on every request.
public class AppAuthStateProvider : AuthenticationStateProvider
{
    private readonly SessionStore _sessionStore;

    public AppAuthStateProvider(SessionStore sessionStore)
    {
        _sessionStore = sessionStore;

        // When the session changes (login / logout / expired), tell Blazor to check again.
        _sessionStore.Changed += OnSessionChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await _sessionStore.GetAsync();

        if (session == null)
        {
            // An identity without an authentication type = not logged in.
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId),
            new Claim(ClaimTypes.Name, session.FullName),
            new Claim(ClaimTypes.Email, session.Email),
            new Claim(ClaimTypes.Role, session.Role)
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void OnSessionChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
