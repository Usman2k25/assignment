using System.Net.Http.Headers;
using System.Net.Http.Json;
using CampusServices.Application.DTOs;

namespace CampusServices.MAUI.Services;

// The ONLY class that talks to the API. Razor pages call these methods and never use HttpClient themselves.
// It is a "typed HttpClient": ASP.NET gives it a ready-made HttpClient (see MauiProgram.cs).
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly SessionStore _sessionStore;

    public ApiClient(HttpClient http, SessionStore sessionStore)
    {
        _http = http;
        _sessionStore = sessionStore;
    }

    // ---------- Login / register (no token needed) ----------

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        return await SendForResultAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/login", dto, useToken: false);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        return await SendForResultAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/register", dto, useToken: false);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var dto = new RefreshDto { RefreshToken = refreshToken };
        await SendAsync(HttpMethod.Post, "api/auth/logout", dto, useToken: false);
    }

    // ---------- Service requests ----------

    public async Task<List<ServiceRequestDto>> GetRequestsAsync()
    {
        return await SendForResultAsync<List<ServiceRequestDto>>(HttpMethod.Get, "api/requests");
    }

    public async Task<ServiceRequestDto> GetRequestAsync(int id)
    {
        return await SendForResultAsync<ServiceRequestDto>(HttpMethod.Get, "api/requests/" + id);
    }

    public async Task<ServiceRequestDto> CreateRequestAsync(CreateRequestDto dto)
    {
        return await SendForResultAsync<ServiceRequestDto>(HttpMethod.Post, "api/requests", dto);
    }

    // Staff only (the API rejects anyone else with 403).
    public async Task<ServiceRequestDto> AssignAsync(int id, AssignRequestDto dto)
    {
        return await SendForResultAsync<ServiceRequestDto>(HttpMethod.Put, "api/requests/" + id + "/assign", dto);
    }

    // Staff only.
    public async Task<ServiceRequestDto> ChangeStatusAsync(int id, ChangeStatusDto dto)
    {
        return await SendForResultAsync<ServiceRequestDto>(HttpMethod.Put, "api/requests/" + id + "/status", dto);
    }

    // ---------- Drop-down data ----------

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await SendForResultAsync<List<CategoryDto>>(HttpMethod.Get, "api/categories");
    }

    public async Task<List<StaffMemberDto>> GetStaffAsync()
    {
        return await SendForResultAsync<List<StaffMemberDto>>(HttpMethod.Get, "api/staff");
    }

    // ---------- How a request is sent ----------

    // Sends the request and converts the JSON answer to T.
    private async Task<T> SendForResultAsync<T>(HttpMethod method, string url, object? body = null, bool useToken = true)
    {
        var response = await SendAsync(method, url, body, useToken);

        var result = await response.Content.ReadFromJsonAsync<T>();
        if (result == null)
        {
            throw new ApiException("The server sent an empty answer.");
        }
        return result;
    }

    // Sends the request. If the API answers with an error, throws ApiException with a friendly message.
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null, bool useToken = true)
    {
        var request = new HttpRequestMessage(method, url);

        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (useToken)
        {
            var token = await GetValidAccessTokenAsync();
            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("Cannot reach the server. Is the API running?");
        }
        catch (TaskCanceledException)
        {
            throw new ApiException("The server took too long to answer.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response);
        }

        return response;
    }

    // ---------- Access token + refresh token ----------

    // An access token only lives 30 minutes. If it is (almost) expired we first swap
    // the refresh token for a new pair, so the user does not have to log in again.
    private async Task<string?> GetValidAccessTokenAsync()
    {
        var session = await _sessionStore.GetAsync();
        if (session == null)
        {
            return null;
        }

        if (session.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
        {
            return session.AccessToken;
        }

        var newSession = await RefreshAsync(session.RefreshToken);
        if (newSession == null)
        {
            // The refresh token is also expired or was revoked: the user must log in again.
            _sessionStore.Clear();
            throw new ApiException("Your session has expired. Please log in again.", 401);
        }

        return newSession.AccessToken;
    }

    private async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
    {
        try
        {
            var dto = new RefreshDto { RefreshToken = refreshToken };
            var response = await _http.PostAsJsonAsync("api/auth/refresh", dto);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var newSession = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (newSession != null)
            {
                await _sessionStore.SaveAsync(newSession);
            }
            return newSession;
        }
        catch (HttpRequestException)
        {
            // No internet / API is down. This does NOT mean the session is invalid, so we do not log out.
            throw new ApiException("Cannot reach the server. Is the API running?");
        }
    }

    // ---------- Turning an error answer into a message ----------

    private static async Task<ApiException> CreateExceptionAsync(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        var message = "The request failed (error " + statusCode + ").";

        if (statusCode == 401) message = "Please log in again.";
        if (statusCode == 403) message = "You are not allowed to do this.";
        if (statusCode == 404) message = "The item was not found.";

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            if (problem != null)
            {
                if (problem.Errors != null && problem.Errors.Count > 0)
                {
                    // Validation errors: { "Title": ["The Title field is required."] }
                    var texts = new List<string>();
                    foreach (var messages in problem.Errors.Values)
                    {
                        texts.AddRange(messages);
                    }
                    message = string.Join(" ", texts);
                }
                else if (!string.IsNullOrWhiteSpace(problem.Detail))
                {
                    message = problem.Detail;
                }
            }
        }
        catch (Exception)
        {
            // The body was empty or not JSON. Keep the default message above.
        }

        return new ApiException(message, statusCode);
    }

    // The shape of the error JSON sent by the API (ProblemDetails / ValidationProblemDetails).
    private class ProblemResponse
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
