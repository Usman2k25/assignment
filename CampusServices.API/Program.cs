using CampusServices.API.Extensions;
using CampusServices.API.Middleware;
using CampusServices.Application.Interfaces;
using CampusServices.Application.Services;
using CampusServices.Infrastructure;
using CampusServices.Infrastructure.Seed;

// Program.cs is the "composition root": it connects all the layers together.
var builder = WebApplication.CreateBuilder(args);

// ----- Register services (Dependency Injection) -----
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);   // database, Identity, repositories, AuthService
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();   // Application use cases
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

// ----- Middleware pipeline (the order matters) -----
app.UseMiddleware<ExceptionHandlingMiddleware>();   // turns exceptions into proper JSON errors

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // In development the MAUI app talks to plain http://localhost, so we only redirect in production.
    app.UseHttpsRedirection();
}

app.UseAuthentication();   // 1. Who are you? (reads the JWT)
app.UseAuthorization();    // 2. Are you allowed? (checks [Authorize] roles)
app.MapControllers();

// ----- Create the roles (and the optional staff account) -----
try
{
    await DbSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Could not seed the database. Did you run 'dotnet ef database update'? (see README.md)");
}

app.Run();

// Makes the Program class visible to the test project (WebApplicationFactory<Program>).
public partial class Program
{
}
