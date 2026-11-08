using CampusServices.Domain.Entities;

namespace CampusServices.Application.Interfaces;

public interface IUserRepository
{
    Task<List<ApplicationUser>> GetStaffAsync();
    Task<bool> IsStaffAsync(string userId);
}
