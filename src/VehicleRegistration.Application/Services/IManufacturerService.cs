using VehicleRegistration.Application.DTOs;

namespace VehicleRegistration.Application.Services;

public interface IManufacturerService
{
    Task<List<ManufacturerResponse>> GetAllManufacturersAsync();
    Task<ManufacturerResponse> CreateManufacturerAsync(string name, CancellationToken ct = default);
    Task<bool> ManufacturerExistsAsync(int id);
}