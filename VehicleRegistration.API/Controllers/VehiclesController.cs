using Microsoft.AspNetCore.Mvc;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Exceptions;
using VehicleRegistration.Application.Services;


namespace VehicleRegistration.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly IOwnerService _ownerService;
    private readonly IManufacturerService _manufacturerService;

    public VehiclesController(
        IVehicleService vehicleService,
        IOwnerService ownerService,
        IManufacturerService manufacturerService)
    {
        _vehicleService = vehicleService;
        _ownerService = ownerService;
        _manufacturerService = manufacturerService;
    }

    private static readonly HashSet<string> AllowedSortFields =
    new(StringComparer.OrdinalIgnoreCase) { "owner", "ownerName", "manufacturer", "year", "weight" };

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VehicleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<VehicleResponse>>>> GetVehicles(
        [FromQuery] string? sortBy = null,
        [FromQuery] bool desc = false)
    {
        if (!string.IsNullOrWhiteSpace(sortBy) && !AllowedSortFields.Contains(sortBy.Trim()))
        {
            throw new BusinessRuleException(
                $"Invalid sortBy value '{sortBy}'. Allowed values: ownerName, manufacturer, year, weight.");
        }
        var vehicles = await _vehicleService.GetAllVehiclesAsync(sortBy, desc);
        return Ok(ApiResponse<List<VehicleResponse>>.Ok(vehicles));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> GetVehicle(int id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle == null)
            throw new NotFoundException("Vehicle", id);

        return Ok(ApiResponse<VehicleResponse>.Ok(vehicle));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> CreateVehicle(
        [FromBody] CreateVehicleRequest request)
    {
        if (!await _ownerService.OwnerExistsAsync(request.OwnerId))
            throw new NotFoundException("Owner", request.OwnerId);

        if (!await _manufacturerService.ManufacturerExistsAsync(request.ManufacturerId))
            throw new NotFoundException("Manufacturer", request.ManufacturerId);

        var vehicle = await _vehicleService.CreateVehicleAsync(request);
        return CreatedAtAction(
            nameof(GetVehicle),
            new { id = vehicle.Id },
            ApiResponse<VehicleResponse>.Ok(vehicle, "Vehicle created successfully."));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> UpdateVehicle(
        int id,
        [FromBody] UpdateVehicleRequest request)
    {
        if (!await _ownerService.OwnerExistsAsync(request.OwnerId))
            throw new NotFoundException("Owner", request.OwnerId);

        if (!await _manufacturerService.ManufacturerExistsAsync(request.ManufacturerId))
            throw new NotFoundException("Manufacturer", request.ManufacturerId);

        var vehicle = await _vehicleService.UpdateVehicleAsync(id, request);
        if (vehicle == null)
            throw new NotFoundException("Vehicle", id);

        return Ok(ApiResponse<VehicleResponse>.Ok(vehicle, "Vehicle updated successfully."));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var deleted = await _vehicleService.DeleteVehicleAsync(id);
        if (!deleted)
            throw new NotFoundException("Vehicle", id);

        return NoContent();
    }
}