using CampusServices.Application.Interfaces;
using CampusServices.Domain.Entities;
using CampusServices.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusServices.Infrastructure.Repositories;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly AppDbContext _db;

    public ServiceRequestRepository(AppDbContext db)
    {
        _db = db;
    }

    // ---------------------------------------------------------------
    // READ-ONLY queries
    //
    // AsNoTracking(): we only want to READ these rows, so EF Core does not need to
    // remember (track) them. This is faster and uses less memory.
    //
    // Include(): loads the related Category, Student and AssignedTo in the SAME SQL query
    // (using JOINs). Without Include we would get the N+1 problem - see docs/ARCHITECTURE.md.
    // ---------------------------------------------------------------
    private IQueryable<ServiceRequest> ReadOnlyQuery()
    {
        return _db.ServiceRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Student)
            .Include(r => r.AssignedTo);
    }

    public async Task<List<ServiceRequest>> GetAllAsync()
    {
        return await ReadOnlyQuery()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ServiceRequest>> GetByStudentAsync(string studentId)
    {
        return await ReadOnlyQuery()
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServiceRequest?> GetByIdAsync(int id)
    {
        return await ReadOnlyQuery().FirstOrDefaultAsync(r => r.Id == id);
    }

    // ---------------------------------------------------------------
    // TRACKED query (no AsNoTracking)
    //
    // EF Core remembers the original values of the returned object.
    // When we change it and call SaveChangesAsync(), EF Core finds the changes
    // and writes an UPDATE for us.
    // ---------------------------------------------------------------
    public async Task<ServiceRequest?> GetByIdForUpdateAsync(int id)
    {
        return await _db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(ServiceRequest request)
    {
        await _db.ServiceRequests.AddAsync(request);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
