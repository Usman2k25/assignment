namespace CampusServices.Domain.Entities;

// A long-lived token used to get a new access token without typing the password again.
// We store only a HASH of the token, so a database leak does not reveal usable tokens.
public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Set when the token is used (rotation) or when the user logs out (revocation).
    public DateTime? RevokedAt { get; set; }

    public bool IsActive
    {
        get { return RevokedAt == null && ExpiresAt > DateTime.UtcNow; }
    }
}
