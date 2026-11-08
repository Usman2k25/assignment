using CampusServices.Domain.Entities;
using CampusServices.Domain.Enums;
using CampusServices.Domain.Exceptions;

namespace CampusServices.Tests;

// Plain unit tests: no API, no database. They test the business rules inside ServiceRequest.
public class ServiceRequestDomainTests
{
    [Theory]
    [InlineData(RequestStatus.Assigned, RequestStatus.InProgress)]
    [InlineData(RequestStatus.Assigned, RequestStatus.Rejected)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Resolved)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Rejected)]
    [InlineData(RequestStatus.Pending, RequestStatus.Rejected)]
    public void ChangeStatus_AllowedTransition_ChangesTheStatus(RequestStatus from, RequestStatus to)
    {
        var request = new ServiceRequest { Status = from, AssignedToId = "staff-1" };

        request.ChangeStatus(to);

        Assert.Equal(to, request.Status);
    }

    [Theory]
    [InlineData(RequestStatus.Pending, RequestStatus.InProgress)]
    [InlineData(RequestStatus.Pending, RequestStatus.Resolved)]
    [InlineData(RequestStatus.Assigned, RequestStatus.Resolved)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Pending)]
    [InlineData(RequestStatus.Resolved, RequestStatus.InProgress)]
    [InlineData(RequestStatus.Resolved, RequestStatus.Rejected)]
    [InlineData(RequestStatus.Rejected, RequestStatus.Assigned)]
    public void ChangeStatus_InvalidTransition_ThrowsDomainException(RequestStatus from, RequestStatus to)
    {
        var request = new ServiceRequest { Status = from };

        Assert.Throws<DomainException>(() => request.ChangeStatus(to));
        Assert.Equal(from, request.Status);   // the status did not change
    }

    [Fact]
    public void ChangeStatus_ToAssigned_MustUseAssign()
    {
        var request = new ServiceRequest { Status = RequestStatus.Pending };

        Assert.Throws<DomainException>(() => request.ChangeStatus(RequestStatus.Assigned));
    }

    [Fact]
    public void Assign_PendingRequest_BecomesAssigned()
    {
        var request = new ServiceRequest { Status = RequestStatus.Pending };

        request.Assign("staff-1");

        Assert.Equal(RequestStatus.Assigned, request.Status);
        Assert.Equal("staff-1", request.AssignedToId);
    }

    [Fact]
    public void Assign_ResolvedRequest_ThrowsDomainException()
    {
        var request = new ServiceRequest { Status = RequestStatus.Resolved };

        Assert.Throws<DomainException>(() => request.Assign("staff-1"));
    }

    [Fact]
    public void Edit_PendingRequest_ChangesTheText()
    {
        var request = new ServiceRequest { Status = RequestStatus.Pending };

        request.Edit("New title", "New description text", 2);

        Assert.Equal("New title", request.Title);
        Assert.Equal(2, request.CategoryId);
    }

    [Fact]
    public void Edit_RequestThatIsNotPending_ThrowsDomainException()
    {
        var request = new ServiceRequest { Status = RequestStatus.InProgress };

        Assert.Throws<DomainException>(() => request.Edit("New title", "New description text", 2));
    }

    [Fact]
    public void StatusRules_FinalStatuses_HaveNoNextStatus()
    {
        Assert.Empty(StatusRules.GetNextStatuses(RequestStatus.Resolved));
        Assert.Empty(StatusRules.GetNextStatuses(RequestStatus.Rejected));
    }
}
