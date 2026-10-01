using Microsoft.AspNetCore.Mvc;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Services;

namespace VehicleRegistration.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManufacturersController : ControllerBase
{
    private readonly IManufacturerService _manufacturerService;

    public ManufacturersController(IManufacturerService manufacturerService)
    {
        _manufacturerService = manufacturerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ManufacturerResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ManufacturerResponse>>>> GetManufacturers()
    {
        var manufacturers = await _manufacturerService.GetAllManufacturersAsync();
        return Ok(ApiResponse<List<ManufacturerResponse>>.Ok(manufacturers));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ManufacturerResponse>>> CreateManufacturer([FromBody] CreateManufacturerRequest request, CancellationToken ct)
    {
        var manufacturer = await _manufacturerService.CreateManufacturerAsync(request.Name, ct);
        return CreatedAtAction(nameof(GetManufacturers), ApiResponse<ManufacturerResponse>.Ok(manufacturer));
    }
}