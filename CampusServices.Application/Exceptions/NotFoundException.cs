namespace CampusServices.Application.Exceptions;

// Thrown when something does not exist. The API turns it into 404 Not Found.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
