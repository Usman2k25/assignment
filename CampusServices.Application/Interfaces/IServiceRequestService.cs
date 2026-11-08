using CampusServices.Application.DTOs;

namespace CampusServices.Application.Interfaces;

// The use cases of the system. Controllers call this interface and nothing else.
public interface IServiceRequestService
{
    Task<ServiceRequestDto> CreateAsync(string studentId, CreateRequestDto dto);

    // A student gets their own requests, staff get all requests.
    Task<List<ServiceRequestDto>> GetListAsync(string userId, bool isStaff);

    Task<ServiceRequestDto> GetByIdAsync(int id, string userId, bool isStaff);

    // Student edits their own Pending request.
    Task<ServiceRequestDto> UpdateAsync(int id, string studentId, UpdateRequestDto dto);

    // Staff only.
    Task<ServiceRequestDto> AssignAsync(int id, AssignRequestDto dto);
    Task<ServiceRequestDto> ChangeStatusAsync(int id, ChangeStatusDto dto);

    // Drop-down data.
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<List<StaffMemberDto>> GetStaffAsync();
}
