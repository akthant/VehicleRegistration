using VehicleRegistration.Application.DTOs;

namespace VehicleRegistration.Application.Services;

public interface IVehicleService
{
    Task<List<VehicleResponse>> GetAllVehiclesAsync(string? sortBy, bool descending);
    Task<VehicleResponse?> GetVehicleByIdAsync(int id);
    Task<VehicleResponse> CreateVehicleAsync(CreateVehicleRequest request);
    Task<VehicleResponse?> UpdateVehicleAsync(int id, UpdateVehicleRequest request);
    Task<bool> DeleteVehicleAsync(int id);
}
