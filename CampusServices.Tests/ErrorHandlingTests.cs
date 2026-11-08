using System.Net;
using System.Net.Http.Headers;
using CampusServices.Application.Interfaces;
using CampusServices.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusServices.Tests;

public class ErrorHandlingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ErrorHandlingTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // A repository that always fails, to simulate an unexpected error (for example the database is down).
    private class BrokenCategoryRepository : ICategoryRepository
    {
        public Task<List<Category>> GetAllAsync()
        {
            throw new InvalidOperationException("secret-database-detail");
        }

        public Task<bool> ExistsAsync(int id)
        {
            throw new InvalidOperationException("secret-database-detail");
        }
    }

    [Fact]
    public async Task UnexpectedError_Returns500_AndDoesNotLeakDetails()
    {
        var student = await _factory.CreateStudentAsync();
        var token = student.Client.DefaultRequestHeaders.Authorization!.Parameter;

        // The same API, but with the broken repository plugged in.
        var brokenFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICategoryRepository>();
                services.AddScoped<ICategoryRepository, BrokenCategoryRepository>();
            });
        });
        var client = brokenFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/categories");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Internal Server Error", body);
        Assert.DoesNotContain("secret-database-detail", body);   // the real error is only logged on the server
    }
}
