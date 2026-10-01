using Microsoft.AspNetCore.Mvc;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Exceptions;
using VehicleRegistration.Application.Services;

namespace VehicleRegistration.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OwnersController : ControllerBase
{
    private readonly IOwnerService _ownerService;

    public OwnersController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OwnerResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<OwnerResponse>>>> GetOwners()
    {
        var owners = await _ownerService.GetAllOwnersAsync();
        return Ok(ApiResponse<List<OwnerResponse>>.Ok(owners));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OwnerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OwnerResponse>>> GetOwner(int id)
    {
        var owner = await _ownerService.GetOwnerByIdAsync(id);
        if (owner == null)
            throw new NotFoundException("Owner", id);

        return Ok(ApiResponse<OwnerResponse>.Ok(owner));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OwnerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OwnerResponse>>> CreateOwner(
        [FromBody] CreateOwnerRequest request)
    {
        var owner = await _ownerService.CreateOwnerAsync(request.Name);
        return CreatedAtAction(
            nameof(GetOwner),
            new { id = owner.Id },
            ApiResponse<OwnerResponse>.Ok(owner, "Owner created successfully."));
    }
}