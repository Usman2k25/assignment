using CampusServices.Application.Interfaces;
using CampusServices.Domain.Entities;
using CampusServices.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusServices.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _db.Categories.AnyAsync(c => c.Id == id);
    }
}
