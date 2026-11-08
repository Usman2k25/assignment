namespace CampusServices.Domain.Enums;

// The ONE place that says which status changes are allowed.
//
//   Pending  -> Assigned or Rejected
//   Assigned -> InProgress or Rejected
//   InProgress -> Resolved or Rejected
//   Resolved / Rejected -> final (no more changes)
public static class StatusRules
{
    public static List<RequestStatus> GetNextStatuses(RequestStatus current)
    {
        switch (current)
        {
            case RequestStatus.Pending:
                return new List<RequestStatus> { RequestStatus.Assigned, RequestStatus.Rejected };

            case RequestStatus.Assigned:
                return new List<RequestStatus> { RequestStatus.InProgress, RequestStatus.Rejected };

            case RequestStatus.InProgress:
                return new List<RequestStatus> { RequestStatus.Resolved, RequestStatus.Rejected };

            default:
                // Resolved and Rejected are final.
                return new List<RequestStatus>();
        }
    }
}
