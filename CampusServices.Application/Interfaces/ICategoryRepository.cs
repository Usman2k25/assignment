using CampusServices.Domain.Entities;

namespace CampusServices.Application.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<bool> ExistsAsync(int id);
}
