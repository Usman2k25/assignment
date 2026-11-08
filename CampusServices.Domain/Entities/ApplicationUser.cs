using Microsoft.AspNetCore.Identity;

namespace CampusServices.Domain.Entities;

// A person who can log in (a student or a staff member).
// IdentityUser already gives us Id, Email, UserName and PasswordHash (never the plain password).
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // Requests this user created (used when the user is a student).
    public List<ServiceRequest> CreatedRequests { get; set; } = new();

    // Requests assigned to this user (used when the user is staff).
    public List<ServiceRequest> AssignedRequests { get; set; } = new();
}
