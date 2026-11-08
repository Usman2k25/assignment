using CampusServices.Domain.Enums;
using CampusServices.Domain.Exceptions;

namespace CampusServices.Domain.Entities;

// One request made by a student (for example "Wi-Fi not working in Library").
// The business rules (who can change what, and when) are methods on this class.
public class ServiceRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Foreign key + navigation property: each request belongs to ONE category.
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // The student who created the request.
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    // The staff member working on it (null until a staff member is assigned).
    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    // A student may change the text only while nobody has started working on it.
    public void Edit(string title, string description, int categoryId)
    {
        if (Status != RequestStatus.Pending)
        {
            throw new DomainException("Only a Pending request can be edited.");
        }

        Title = title;
        Description = description;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    // Assigning is allowed for a new request (Pending) or to re-assign (Assigned).
    public void Assign(string staffId)
    {
        if (Status != RequestStatus.Pending && Status != RequestStatus.Assigned)
        {
            throw new DomainException("A request that is " + Status + " cannot be assigned.");
        }

        AssignedToId = staffId;
        Status = RequestStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(RequestStatus newStatus)
    {
        var allowed = StatusRules.GetNextStatuses(Status);

        if (!allowed.Contains(newStatus))
        {
            throw new DomainException("A request cannot change from " + Status + " to " + newStatus + ".");
        }

        // "Assigned" needs a staff member, so it must be done with Assign().
        if (newStatus == RequestStatus.Assigned)
        {
            throw new DomainException("To set the status to Assigned, assign the request to a staff member.");
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
