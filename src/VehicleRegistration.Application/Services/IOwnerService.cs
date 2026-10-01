using VehicleRegistration.Application.DTOs;

namespace VehicleRegistration.Application.Services;

public interface IOwnerService
{
    Task<List<OwnerResponse>> GetAllOwnersAsync();
    Task<OwnerResponse?> GetOwnerByIdAsync(int id);
    Task<OwnerResponse> CreateOwnerAsync(string name);
    Task<bool> OwnerExistsAsync(int id);
}