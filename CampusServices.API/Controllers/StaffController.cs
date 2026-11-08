using CampusServices.Application.DTOs;
using CampusServices.Application.Interfaces;
using CampusServices.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusServices.API.Controllers;

// The list of staff members (used for the "Assign to" drop-down). Staff only.
[ApiController]
[Route("api/staff")]
[Authorize(Roles = Roles.Staff)]
public class StaffController : ControllerBase
{
    private readonly IServiceRequestService _service;

    public StaffController(IServiceRequestService service)
    {
        _service = service;
    }

    // GET api/staff
    [HttpGet]
    [ProducesResponseType(typeof(List<StaffMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<StaffMemberDto>>> GetAll()
    {
        var staff = await _service.GetStaffAsync();
        return Ok(staff);
    }
}
