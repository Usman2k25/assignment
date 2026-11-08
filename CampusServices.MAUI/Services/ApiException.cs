namespace CampusServices.MAUI.Services;

// Thrown by ApiClient when the API answers with an error or cannot be reached.
// The Message is already friendly, so a page can show it directly to the user.
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode = 0) : base(message)
    {
        StatusCode = statusCode;
    }
}
