namespace CampusServices.Domain.Entities;

// IT Support, Facilities or Administration.
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<ServiceRequest> ServiceRequests { get; set; } = new();
}
