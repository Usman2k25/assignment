using System.Text.Json.Serialization;

namespace CampusServices.Domain.Enums;

// The attribute makes JSON show "InProgress" instead of the number 2,
// so the API, Swagger and the MAUI app all use readable text.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
{
    Pending,
    Assigned,
    InProgress,
    Resolved,
    Rejected
}
