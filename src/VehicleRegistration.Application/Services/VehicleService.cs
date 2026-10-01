using Microsoft.EntityFrameworkCore;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Exceptions;
using VehicleRegistration.Application.Services;
using VehicleRegistration.Core.Entities;
using VehicleRegistration.Infrastructure.Data;

namespace VehicleRegistration.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly VehicleDbContext _context;
    private readonly ICategoryService _categoryService;

    public VehicleService(VehicleDbContext context, ICategoryService categoryService)
    {
        _context = context;
        _categoryService = categoryService;
    }

    public async Task<List<VehicleResponse>> GetAllVehiclesAsync(string? sortBy, bool descending)
    {
        
        var categories = await _context.VehicleCategories
            .AsNoTracking()
            .OrderBy(c => c.MinWeight)
            .ToListAsync();

        IQueryable<Vehicle> query = _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Owner)
            .Include(v => v.Manufacturer);

        query = sortBy?.Trim().ToLowerInvariant() switch
        {
            "owner" or "ownername" => descending
                ? query.OrderByDescending(v => v.Owner.Name)
                : query.OrderBy(v => v.Owner.Name),
            "manufacturer" => descending
                ? query.OrderByDescending(v => v.Manufacturer.Name)
                : query.OrderBy(v => v.Manufacturer.Name),
            "year" => descending
                ? query.OrderByDescending(v => v.YearOfManufacture)
                : query.OrderBy(v => v.YearOfManufacture),
            "weight" => descending
                ? query.OrderByDescending(v => v.Weight)
                : query.OrderBy(v => v.Weight),
            _ => query.OrderBy(v => v.Id)
        };

        var vehicles = await query.ToListAsync();

        return vehicles.Select(v =>
        {
            var category = FindCategoryForWeight(categories, v.Weight);
            return new VehicleResponse(
                v.Id,
                v.Owner.Name,
                v.Manufacturer.Name,
                v.YearOfManufacture,
                v.Weight,
                category?.Name ?? "Unknown",
                category?.IconUrl ?? string.Empty);
        }).ToList();
    }

    public async Task<VehicleResponse?> GetVehicleByIdAsync(int id)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Owner)
            .Include(v => v.Manufacturer)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle == null) return null;

        var category = await _categoryService.GetCategoryForWeightAsync(vehicle.Weight);

        return new VehicleResponse(
            vehicle.Id,
            vehicle.Owner.Name,
            vehicle.Manufacturer.Name,
            vehicle.YearOfManufacture,
            vehicle.Weight,
            category?.Name ?? "Unknown",
            category?.IconUrl ?? string.Empty);
    }

    public async Task<VehicleResponse> CreateVehicleAsync(CreateVehicleRequest request)
    {
        var vehicle = new Vehicle
        {
            OwnerId = request.OwnerId,
            ManufacturerId = request.ManufacturerId,
            YearOfManufacture = request.YearOfManufacture,
            Weight = request.Weight
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return (await GetVehicleByIdAsync(vehicle.Id))!;
    }

    public async Task<VehicleResponse?> UpdateVehicleAsync(int id, UpdateVehicleRequest request)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null) return null;

        vehicle.OwnerId = request.OwnerId;
        vehicle.ManufacturerId = request.ManufacturerId;
        vehicle.YearOfManufacture = request.YearOfManufacture;
        vehicle.Weight = request.Weight;

        await _context.SaveChangesAsync();

        return await GetVehicleByIdAsync(id);
    }

    public async Task<bool> DeleteVehicleAsync(int id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null) return false;

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();
        return true;
    }

    private static VehicleCategory? FindCategoryForWeight(List<VehicleCategory> categories, decimal weight)
    {
        return categories.FirstOrDefault(c =>
            c.MinWeight <= weight && (c.MaxWeight == null || c.MaxWeight >= weight));
    }
}