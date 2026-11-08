using CampusServices.Domain.Entities;

namespace CampusServices.Application.Interfaces;

// Defined here (inner layer). Implemented in Infrastructure (outer layer) with EF Core.
// That is the Dependency Inversion Principle: Application does not know about EF Core or PostgreSQL.
public interface IServiceRequestRepository
{
    // Read-only queries (no change tracking). They include Category, Student and AssignedTo.
    Task<List<ServiceRequest>> GetAllAsync();
    Task<List<ServiceRequest>> GetByStudentAsync(string studentId);
    Task<ServiceRequest?> GetByIdAsync(int id);

    // Tracked query: use this when you are going to change the request and save it.
    Task<ServiceRequest?> GetByIdForUpdateAsync(int id);

    Task AddAsync(ServiceRequest request);
    Task SaveChangesAsync();
}
