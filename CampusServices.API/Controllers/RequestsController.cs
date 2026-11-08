using System.Security.Claims;
using CampusServices.Application.DTOs;
using CampusServices.Application.Interfaces;
using CampusServices.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusServices.API.Controllers;

// [Authorize] on the class: you must send a valid JWT for every endpoint here (otherwise 401).
// [Authorize(Roles = ...)] on an action: the JWT must also contain that role (otherwise 403).
// This is checked on the SERVER, so hiding a button in the app is not the real protection.
[ApiController]
[Route("api/requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IServiceRequestService _service;

    public RequestsController(IServiceRequestService service)
    {
        _service = service;
    }

    // The id and role come from the JWT claims, never from the request body.
    private string CurrentUserId
    {
        get { return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty; }
    }

    private bool IsStaff
    {
        get { return User.IsInRole(Roles.Staff); }
    }

    // POST api/requests   (students only)
    [HttpPost]
    [Authorize(Roles = Roles.Student)]
    [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServiceRequestDto>> Create(CreateRequestDto dto)
    {
        var created = await _service.CreateAsync(CurrentUserId, dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // GET api/requests   (student: own requests, staff: all requests)
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ServiceRequestDto>>> GetAll()
    {
        var requests = await _service.GetListAsync(CurrentUserId, IsStaff);
        return Ok(requests);
    }

    // GET api/requests/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDto>> GetById(int id)
    {
        var request = await _service.GetByIdAsync(id, CurrentUserId, IsStaff);
        return Ok(request);
    }

    // PUT api/requests/5   (a student edits their own Pending request)
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Student)]
    [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDto>> Update(int id, UpdateRequestDto dto)
    {
        var updated = await _service.UpdateAsync(id, CurrentUserId, dto);
        return Ok(updated);
    }

    // PUT api/requests/5/assign   (staff only)
    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDto>> Assign(int id, AssignRequestDto dto)
    {
        var updated = await _service.AssignAsync(id, dto);
        return Ok(updated);
    }

    // PUT api/requests/5/status   (staff only)
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDto>> ChangeStatus(int id, ChangeStatusDto dto)
    {
        var updated = await _service.ChangeStatusAsync(id, dto);
        return Ok(updated);
    }
}
