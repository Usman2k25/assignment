using CampusServices.Application.DTOs;
using CampusServices.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusServices.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IServiceRequestService _service;

    public CategoriesController(IServiceRequestService service)
    {
        _service = service;
    }

    // GET api/categories
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var categories = await _service.GetCategoriesAsync();
        return Ok(categories);
    }
}
