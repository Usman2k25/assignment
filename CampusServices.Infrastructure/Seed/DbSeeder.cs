using CampusServices.Domain;
using CampusServices.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusServices.Infrastructure.Seed;

// Runs every time the API starts. It is safe to run many times (it checks before it creates).
//  1. Creates the two roles: Student and Staff.
//  2. Creates ONE staff account, but only if SeedStaff:Email and SeedStaff:Password
//     exist in the configuration (user-secrets). No password is stored in the code.
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await CreateRoleIfMissingAsync(roleManager, Roles.Student);
        await CreateRoleIfMissingAsync(roleManager, Roles.Staff);

        await CreateStaffIfConfiguredAsync(userManager, configuration);
    }

    private static async Task CreateRoleIfMissingAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        var exists = await roleManager.RoleExistsAsync(roleName);
        if (!exists)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task CreateStaffIfConfiguredAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var email = configuration["SeedStaff:Email"];
        var password = configuration["SeedStaff:Password"];
        var fullName = configuration["SeedStaff:FullName"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            return;
        }

        var staff = new ApplicationUser();
        staff.UserName = email;
        staff.Email = email;
        staff.FullName = string.IsNullOrWhiteSpace(fullName) ? "Staff Member" : fullName;

        var result = await userManager.CreateAsync(staff, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(staff, Roles.Staff);
        }
    }
}
