using System.Net;
using System.Net.Http.Json;
using CampusServices.Application.DTOs;
using CampusServices.Domain.Enums;

namespace CampusServices.Tests;

// Each test starts the API, calls it with HttpClient and checks the HTTP status code.
public class ServiceRequestApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ServiceRequestApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static CreateRequestDto ValidRequest()
    {
        return new CreateRequestDto
        {
            Title = "Wi-Fi is down",
            Description = "The Wi-Fi in the library has not worked since morning.",
            CategoryId = 1
        };
    }

    // Helper: the student creates a request and we return what the API answered.
    private static async Task<ServiceRequestDto> CreateRequestAsync(TestUser student)
    {
        var response = await student.Client.PostAsJsonAsync("/api/requests", ValidRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ServiceRequestDto>();
        return created!;
    }

    // ---------- 1. Valid request creation ----------

    [Fact]
    public async Task Create_WithValidData_Returns201AndPendingRequest()
    {
        var student = await _factory.CreateStudentAsync();

        var response = await student.Client.PostAsJsonAsync("/api/requests", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ServiceRequestDto>();
        Assert.NotNull(created);
        Assert.Equal("Wi-Fi is down", created.Title);
        Assert.Equal("IT Support", created.CategoryName);
        Assert.Equal(RequestStatus.Pending, created.Status);
        Assert.Equal(student.Id, created.StudentId);
    }

    // ---------- 2. Invalid request (validation) ----------

    [Fact]
    public async Task Create_WithInvalidData_Returns400()
    {
        var student = await _factory.CreateStudentAsync();

        var badRequest = new CreateRequestDto
        {
            Title = "",          // required
            Description = "short", // at least 10 characters
            CategoryId = 0       // must pick a category
        };

        var response = await student.Client.PostAsJsonAsync("/api/requests", badRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownCategory_Returns400()
    {
        var student = await _factory.CreateStudentAsync();

        var request = ValidRequest();
        request.CategoryId = 999;

        var response = await student.Client.PostAsJsonAsync("/api/requests", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- 3. Not logged in ----------

    [Fact]
    public async Task GetRequests_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();   // no JWT

        var response = await client.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRequest_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/requests", ValidRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- 4. Student tries a staff-only operation ----------

    [Fact]
    public async Task Student_CannotAssignRequest_Returns403()
    {
        var student = await _factory.CreateStudentAsync();
        var staff = await _factory.CreateStaffAsync();
        var created = await CreateRequestAsync(student);

        var response = await student.Client.PutAsJsonAsync(
            "/api/requests/" + created.Id + "/assign",
            new AssignRequestDto { StaffId = staff.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotChangeStatus_Returns403()
    {
        var student = await _factory.CreateStudentAsync();
        var created = await CreateRequestAsync(student);

        var response = await student.Client.PutAsJsonAsync(
            "/api/requests/" + created.Id + "/status",
            new ChangeStatusDto { Status = RequestStatus.Rejected });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotListStaffMembers_Returns403()
    {
        var student = await _factory.CreateStudentAsync();

        var response = await student.Client.GetAsync("/api/staff");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- 5. Student tries to use another student's request ----------

    [Fact]
    public async Task Student_CannotViewAnotherStudentsRequest_Returns403()
    {
        var owner = await _factory.CreateStudentAsync();
        var otherStudent = await _factory.CreateStudentAsync();
        var created = await CreateRequestAsync(owner);

        var response = await otherStudent.Client.GetAsync("/api/requests/" + created.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotEditAnotherStudentsRequest_Returns403()
    {
        var owner = await _factory.CreateStudentAsync();
        var otherStudent = await _factory.CreateStudentAsync();
        var created = await CreateRequestAsync(owner);

        var changes = new UpdateRequestDto
        {
            Title = "Changed by someone else",
            Description = "I should not be able to change this request.",
            CategoryId = 2
        };
        var response = await otherStudent.Client.PutAsJsonAsync("/api/requests/" + created.Id, changes);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StudentList_ContainsOnlyTheirOwnRequests()
    {
        var studentA = await _factory.CreateStudentAsync();
        var studentB = await _factory.CreateStudentAsync();
        await CreateRequestAsync(studentA);
        await CreateRequestAsync(studentB);

        var list = await studentA.Client.GetFromJsonAsync<List<ServiceRequestDto>>("/api/requests");

        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal(studentA.Id, list[0].StudentId);
    }

    // ---------- 6. Request that does not exist ----------

    [Fact]
    public async Task GetRequest_ThatDoesNotExist_Returns404()
    {
        var student = await _factory.CreateStudentAsync();

        var response = await student.Client.GetAsync("/api/requests/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Staff_ChangingStatusOfMissingRequest_Returns404()
    {
        var staff = await _factory.CreateStaffAsync();

        var response = await staff.Client.PutAsJsonAsync(
            "/api/requests/99999/status",
            new ChangeStatusDto { Status = RequestStatus.Rejected });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- 7. Invalid status transition ----------

    [Fact]
    public async Task Staff_InvalidStatusTransition_Returns400()
    {
        var student = await _factory.CreateStudentAsync();
        var staff = await _factory.CreateStaffAsync();
        var created = await CreateRequestAsync(student);

        // Pending -> Resolved is not allowed (it must be assigned and in progress first).
        var response = await staff.Client.PutAsJsonAsync(
            "/api/requests/" + created.Id + "/status",
            new ChangeStatusDto { Status = RequestStatus.Resolved });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Staff_StatusValueThatDoesNotExist_Returns400()
    {
        var student = await _factory.CreateStudentAsync();
        var staff = await _factory.CreateStaffAsync();
        var created = await CreateRequestAsync(student);

        var json = new StringContent("{\"status\": \"Banana\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await staff.Client.PutAsync("/api/requests/" + created.Id + "/status", json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Staff workflow (happy path) ----------

    [Fact]
    public async Task Staff_CanAssignAndMoveRequestToResolved()
    {
        var student = await _factory.CreateStudentAsync();
        var staff = await _factory.CreateStaffAsync();
        var created = await CreateRequestAsync(student);

        // Staff can see the student's request in the full list.
        var all = await staff.Client.GetFromJsonAsync<List<ServiceRequestDto>>("/api/requests");
        Assert.Contains(all!, r => r.Id == created.Id);

        // Assign -> status becomes Assigned
        var assignResponse = await staff.Client.PutAsJsonAsync(
            "/api/requests/" + created.Id + "/assign",
            new AssignRequestDto { StaffId = staff.Id });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var assigned = await assignResponse.Content.ReadFromJsonAsync<ServiceRequestDto>();
        Assert.Equal(RequestStatus.Assigned, assigned!.Status);
        Assert.Equal("Test Staff", assigned.AssignedToName);

        // Assigned -> InProgress -> Resolved
        var inProgress = await staff.Client.PutAsJsonAsync(
            "/api/requests/" + created.Id + "/status",
            new ChangeStatusDto { Status = RequestStatus.InProgress });
        Assert.Equal(HttpStatusCode.OK, inProgress.StatusCode);

        var resolved = await staff.Client.PutAsJsonAsync(
            "/api/requests/" + created.Id + "/status",
            new ChangeStatusDto { Status = RequestStatus.Resolved });
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);

        // The student can follow the progress.
        var final = await student.Client.GetFromJsonAsync<ServiceRequestDto>("/api/requests/" + created.Id);
        Assert.Equal(RequestStatus.Resolved, final!.Status);
    }

    [Fact]
    public async Task Staff_CannotAssignToSomeoneWhoIsNotStaff_Returns400()
    {
        var student = await _factory.CreateStudentAsync();
        var staff = await _factory.CreateStaffAsync();
        var created = await CreateRequestAsync(student);

        var response = await staff.Client.PutAsJsonAsync(
            "/api/requests/" + created.Id + "/assign",
            new AssignRequestDto { StaffId = student.Id });   // a student, not staff

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Student_CanEditOwnPendingRequest()
    {
        var student = await _factory.CreateStudentAsync();
        var created = await CreateRequestAsync(student);

        var changes = new UpdateRequestDto
        {
            Title = "Wi-Fi is down (updated)",
            Description = "Updated description with more detail about the problem.",
            CategoryId = 2
        };
        var response = await student.Client.PutAsJsonAsync("/api/requests/" + created.Id, changes);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ServiceRequestDto>();
        Assert.Equal("Wi-Fi is down (updated)", updated!.Title);
        Assert.Equal("Facilities", updated.CategoryName);
    }
}
