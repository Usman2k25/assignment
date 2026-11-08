using CampusServices.Application.DTOs;
using CampusServices.Application.Exceptions;
using CampusServices.Application.Interfaces;
using CampusServices.Domain.Entities;
using CampusServices.Domain.Exceptions;

namespace CampusServices.Application.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IServiceRequestRepository _requests;
    private readonly ICategoryRepository _categories;
    private readonly IUserRepository _users;

    // Dependency Injection: ASP.NET Core gives us the three repositories.
    public ServiceRequestService(
        IServiceRequestRepository requests,
        ICategoryRepository categories,
        IUserRepository users)
    {
        _requests = requests;
        _categories = categories;
        _users = users;
    }

    public async Task<ServiceRequestDto> CreateAsync(string studentId, CreateRequestDto dto)
    {
        await CheckCategoryExistsAsync(dto.CategoryId);

        var request = new ServiceRequest();
        request.Title = dto.Title.Trim();
        request.Description = dto.Description.Trim();
        request.CategoryId = dto.CategoryId;
        request.StudentId = studentId;

        await _requests.AddAsync(request);
        await _requests.SaveChangesAsync();

        return await GetDtoAsync(request.Id);
    }

    public async Task<List<ServiceRequestDto>> GetListAsync(string userId, bool isStaff)
    {
        List<ServiceRequest> requests;

        if (isStaff)
        {
            requests = await _requests.GetAllAsync();
        }
        else
        {
            requests = await _requests.GetByStudentAsync(userId);
        }

        var result = new List<ServiceRequestDto>();
        foreach (var request in requests)
        {
            result.Add(ToDto(request));
        }
        return result;
    }

    public async Task<ServiceRequestDto> GetByIdAsync(int id, string userId, bool isStaff)
    {
        var request = await _requests.GetByIdAsync(id);
        if (request == null)
        {
            throw new NotFoundException("Request " + id + " was not found.");
        }

        // Staff can see everything. A student can only see their own request.
        if (!isStaff && request.StudentId != userId)
        {
            throw new ForbiddenException("You can only view your own requests.");
        }

        return ToDto(request);
    }

    public async Task<ServiceRequestDto> UpdateAsync(int id, string studentId, UpdateRequestDto dto)
    {
        // Tracked query: EF Core remembers the original values of this object.
        var request = await _requests.GetByIdForUpdateAsync(id);
        if (request == null)
        {
            throw new NotFoundException("Request " + id + " was not found.");
        }

        if (request.StudentId != studentId)
        {
            throw new ForbiddenException("You can only edit your own requests.");
        }

        await CheckCategoryExistsAsync(dto.CategoryId);

        request.Edit(dto.Title.Trim(), dto.Description.Trim(), dto.CategoryId);

        // CHANGE TRACKING: EF Core compares the object with its original values
        // and creates an UPDATE statement for the changed columns only.
        await _requests.SaveChangesAsync();

        return await GetDtoAsync(id);
    }

    public async Task<ServiceRequestDto> AssignAsync(int id, AssignRequestDto dto)
    {
        var request = await _requests.GetByIdForUpdateAsync(id);
        if (request == null)
        {
            throw new NotFoundException("Request " + id + " was not found.");
        }

        var isStaff = await _users.IsStaffAsync(dto.StaffId);
        if (!isStaff)
        {
            throw new DomainException("The selected user is not a staff member.");
        }

        request.Assign(dto.StaffId);
        await _requests.SaveChangesAsync();

        return await GetDtoAsync(id);
    }

    public async Task<ServiceRequestDto> ChangeStatusAsync(int id, ChangeStatusDto dto)
    {
        var request = await _requests.GetByIdForUpdateAsync(id);
        if (request == null)
        {
            throw new NotFoundException("Request " + id + " was not found.");
        }

        // The Domain checks the rule and throws DomainException for an invalid change.
        request.ChangeStatus(dto.Status);
        await _requests.SaveChangesAsync();

        return await GetDtoAsync(id);
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _categories.GetAllAsync();

        var result = new List<CategoryDto>();
        foreach (var category in categories)
        {
            result.Add(new CategoryDto { Id = category.Id, Name = category.Name });
        }
        return result;
    }

    public async Task<List<StaffMemberDto>> GetStaffAsync()
    {
        var staff = await _users.GetStaffAsync();

        var result = new List<StaffMemberDto>();
        foreach (var person in staff)
        {
            result.Add(new StaffMemberDto
            {
                Id = person.Id,
                FullName = person.FullName,
                Email = person.Email ?? string.Empty
            });
        }
        return result;
    }

    // ---------- private helpers ----------

    private async Task CheckCategoryExistsAsync(int categoryId)
    {
        var exists = await _categories.ExistsAsync(categoryId);
        if (!exists)
        {
            throw new DomainException("The selected category does not exist.");
        }
    }

    // After saving we read the request again so the names (category, student, staff) are filled in.
    private async Task<ServiceRequestDto> GetDtoAsync(int id)
    {
        var request = await _requests.GetByIdAsync(id);
        if (request == null)
        {
            throw new NotFoundException("Request " + id + " was not found.");
        }
        return ToDto(request);
    }

    private static ServiceRequestDto ToDto(ServiceRequest request)
    {
        var dto = new ServiceRequestDto();
        dto.Id = request.Id;
        dto.Title = request.Title;
        dto.Description = request.Description;
        dto.Status = request.Status;
        dto.CreatedAt = request.CreatedAt;
        dto.UpdatedAt = request.UpdatedAt;
        dto.CategoryId = request.CategoryId;
        dto.CategoryName = request.Category?.Name ?? string.Empty;
        dto.StudentId = request.StudentId;
        dto.StudentName = request.Student?.FullName ?? string.Empty;
        dto.AssignedToId = request.AssignedToId;
        dto.AssignedToName = request.AssignedTo?.FullName;
        return dto;
    }
}
