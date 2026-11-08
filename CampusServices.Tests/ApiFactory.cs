using System.Net.Http.Headers;
using System.Net.Http.Json;
using CampusServices.Application.DTOs;
using CampusServices.Domain;
using CampusServices.Domain.Entities;
using CampusServices.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CampusServices.Tests;

// A logged-in test user: an HttpClient that already sends the JWT, and the user's id.
public class TestUser
{
    public HttpClient Client { get; set; } = null!;
    public string Id { get; set; } = string.Empty;
}

// Starts the REAL API in memory for the tests (same controllers, JWT, middleware).
// Only the database is swapped: an in-memory database instead of PostgreSQL.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestDb-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // A fake key that is only used by the tests.
        builder.UseSetting("Jwt:Key", "unit-test-key-that-is-longer-than-32-characters");

        builder.ConfigureServices(services =>
        {
            // Remove the PostgreSQL setup and use the in-memory database instead.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Make sure the three categories exist (a real database gets them from the migration).
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        if (!db.Categories.Any())
        {
            db.Categories.Add(new Category { Id = 1, Name = "IT Support" });
            db.Categories.Add(new Category { Id = 2, Name = "Facilities" });
            db.Categories.Add(new Category { Id = 3, Name = "Administration" });
            db.SaveChanges();
        }

        return host;
    }

    // Registers a new student through the API and returns a client that is logged in.
    public async Task<TestUser> CreateStudentAsync()
    {
        var client = CreateClient();

        var dto = new RegisterDto
        {
            FullName = "Test Student",
            Email = "student-" + Guid.NewGuid().ToString("N") + "@test.com",
            Password = "Student@123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", dto);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return new TestUser { Client = client, Id = auth.UserId };
    }

    // Staff cannot register through the API, so the test creates the staff user directly,
    // then logs in through the API like a real staff member would.
    public async Task<TestUser> CreateStaffAsync()
    {
        var email = "staff-" + Guid.NewGuid().ToString("N") + "@test.com";
        var password = "Staff@12345";

        using (var scope = Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var staff = new ApplicationUser { UserName = email, Email = email, FullName = "Test Staff" };

            var result = await userManager.CreateAsync(staff, password);
            Assert.True(result.Succeeded);
            await userManager.AddToRoleAsync(staff, Roles.Staff);
        }

        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return new TestUser { Client = client, Id = auth.UserId };
    }
}
