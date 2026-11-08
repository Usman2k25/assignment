namespace CampusServices.Application.DTOs;

// Used to fill the "Category" drop-down.
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Used to fill the "Assign to" drop-down (staff only).
public class StaffMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
