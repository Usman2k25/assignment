using System.Net;
using System.Net.Http.Json;
using CampusServices.Application.DTOs;

namespace CampusServices.Tests;

public class AuthApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static RegisterDto NewStudent()
    {
        return new RegisterDto
        {
            FullName = "Auth Student",
            Email = "auth-" + Guid.NewGuid().ToString("N") + "@test.com",
            Password = "Student@123"
        };
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsTokensForStudentRole()
    {
        var client = _factory.CreateClient();
        var student = NewStudent();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", student);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Email = student.Email, Password = student.Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrEmpty(auth!.AccessToken));
        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));
        Assert.Equal("Student", auth.Role);
    }

    [Fact]
    public async Task Register_CannotAskForStaffRole()
    {
        var client = _factory.CreateClient();

        // The API has no "role" field. Even if a hacker adds one, it is ignored.
        var body = new
        {
            fullName = "Sneaky Student",
            email = "sneaky-" + Guid.NewGuid().ToString("N") + "@test.com",
            password = "Student@123",
            role = "Staff"
        };
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.Equal("Student", auth!.Role);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var student = NewStudent();
        student.Password = "abcdef";   // long enough, but no digit / capital letter / symbol

        var response = await client.PostAsJsonAsync("/api/auth/register", student);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithSameEmailTwice_Returns400()
    {
        var client = _factory.CreateClient();
        var student = NewStudent();

        await client.PostAsJsonAsync("/api/auth/register", student);
        var second = await client.PostAsJsonAsync("/api/auth/register", student);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var student = NewStudent();
        await client.PostAsJsonAsync("/api/auth/register", student);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Email = student.Email, Password = "Wrong@12345" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_AfterFiveWrongPasswords_LocksTheAccount()
    {
        var client = _factory.CreateClient();
        var student = NewStudent();
        await client.PostAsJsonAsync("/api/auth/register", student);

        // Identity locks the account after 5 failed attempts.
        for (int i = 0; i < 5; i++)
        {
            var wrong = await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginDto { Email = student.Email, Password = "Wrong@12345" });
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        // Even the CORRECT password is refused while the account is locked.
        var correctButLocked = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Email = student.Email, Password = student.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, correctButLocked.StatusCode);
    }

    [Fact]
    public async Task Refresh_GivesNewTokens_AndTheOldRefreshTokenStopsWorking()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", NewStudent());
        var first = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Use the refresh token once: we get a NEW pair.
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshDto { RefreshToken = first!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var second = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotEqual(first.RefreshToken, second!.RefreshToken);

        // ROTATION: the old refresh token is used up.
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshDto { RefreshToken = first.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // The new one still works.
        var again = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshDto { RefreshToken = second.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", NewStudent());
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new RefreshDto { RefreshToken = auth!.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshDto { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithRandomText_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshDto { RefreshToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
