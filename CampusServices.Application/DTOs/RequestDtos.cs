using System.ComponentModel.DataAnnotations;
using CampusServices.Domain.Enums;

namespace CampusServices.Application.DTOs;

// Data a student sends to create a request.
public class CreateRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    public int CategoryId { get; set; }
}

// Data a student sends to edit their own Pending request.
public class UpdateRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    public int CategoryId { get; set; }
}

// Staff only: which staff member should work on the request.
public class AssignRequestDto
{
    [Required]
    public string StaffId { get; set; } = string.Empty;
}

// Staff only: the new status.
public class ChangeStatusDto
{
    [EnumDataType(typeof(RequestStatus))]
    public RequestStatus Status { get; set; }
}

// What the API sends back. We never send the EF Core entity directly.
public class ServiceRequestDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;

    public string? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
}
