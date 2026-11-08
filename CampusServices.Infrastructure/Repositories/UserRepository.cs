using CampusServices.Application.Interfaces;
using CampusServices.Domain;
using CampusServices.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CampusServices.Infrastructure.Repositories;

// Looks up users through ASP.NET Core Identity's UserManager.
public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<List<ApplicationUser>> GetStaffAsync()
    {
        var staff = await _userManager.GetUsersInRoleAsync(Roles.Staff);

        var sorted = new List<ApplicationUser>(staff);
        sorted.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.OrdinalIgnoreCase));
        return sorted;
    }

    public async Task<bool> IsStaffAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, Roles.Staff);
    }
}
