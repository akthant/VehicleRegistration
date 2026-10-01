using Microsoft.EntityFrameworkCore;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Core.Entities;
using VehicleRegistration.Infrastructure.Data;

namespace VehicleRegistration.Application.Services;

public class OwnerService : IOwnerService
{
    private readonly VehicleDbContext _context;

    public OwnerService(VehicleDbContext context)
    {
        _context = context;
    }

    public async Task<List<OwnerResponse>> GetAllOwnersAsync()
    {
        return await _context.Owners
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new OwnerResponse(o.Id, o.Name))
            .ToListAsync();
    }

    public async Task<OwnerResponse?> GetOwnerByIdAsync(int id)
    {
        var owner = await _context.Owners.FindAsync(id);
        return owner == null ? null : new OwnerResponse(owner.Id, owner.Name);
    }

    public async Task<OwnerResponse> CreateOwnerAsync(string name)
    {
        var owner = new Owner { Name = name };
        _context.Owners.Add(owner);
        await _context.SaveChangesAsync();
        return new OwnerResponse(owner.Id, owner.Name);
    }

    public async Task<bool> OwnerExistsAsync(int id)
    {
        return await _context.Owners.AnyAsync(o => o.Id == id);
    }
}