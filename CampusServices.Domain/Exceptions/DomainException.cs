namespace CampusServices.Domain.Exceptions;

// Thrown when a business rule is broken (for example an invalid status change).
// The API turns it into a 400 Bad Request.
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
