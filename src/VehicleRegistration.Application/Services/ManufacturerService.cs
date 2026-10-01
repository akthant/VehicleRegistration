using Microsoft.EntityFrameworkCore;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Exceptions;
using VehicleRegistration.Core.Entities;
using VehicleRegistration.Infrastructure.Data;
namespace VehicleRegistration.Application.Services;

public class ManufacturerService : IManufacturerService
{
    private readonly VehicleDbContext _context;

    public ManufacturerService(VehicleDbContext context)
    {
        _context = context;
    }

    public async Task<List<ManufacturerResponse>> GetAllManufacturersAsync()
    {
        return await _context.Manufacturers
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new ManufacturerResponse(m.Id, m.Name))
            .ToListAsync();
    }

    public async Task<bool> ManufacturerExistsAsync(int id)
    {
        return await _context.Manufacturers.AnyAsync(m => m.Id == id);
    }

    public async Task<ManufacturerResponse> CreateManufacturerAsync(string name, CancellationToken ct = default)
    {
        var ManufacturerName = name.Trim();
        var manufacturer = new Manufacturer { Name = ManufacturerName };

        var alreadyExists = await _context.Manufacturers
            .AnyAsync(m => m.Name == ManufacturerName, ct);

        if (alreadyExists)
        {
            throw new BusinessRuleException(
                $"Manufacturer '{name}' already exists.");
        }

        _context.Manufacturers.Add(manufacturer);
        await _context.SaveChangesAsync(ct);

        return new ManufacturerResponse(manufacturer.Id, manufacturer.Name);
    }

}