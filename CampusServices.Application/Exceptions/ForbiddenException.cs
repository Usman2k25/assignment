namespace CampusServices.Application.Exceptions;

// Thrown when the user is logged in but is not allowed to do this
// (for example a student opening another student's request). The API turns it into 403 Forbidden.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
