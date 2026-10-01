using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VehicleRegistration.Core.Entities;
using VehicleRegistration.Infrastructure.Data;

namespace VehicleRegistration.IntegrationTests;

public static class TestDbContextFactory
{
    /// <summary>Creates a fresh in-memory database for each test.</summary>
    public static VehicleDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<VehicleDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new VehicleDbContext(options);
        //context.Database.EnsureCreated();
        return context;
    }

    /// <summary>Seeds the standard three-category configuration.</summary>
    public static async Task SeedStandardCategoriesAsync(VehicleDbContext context)
    {
        if (await context.VehicleCategories.AnyAsync())
            return; // already seeded via HasData

        context.VehicleCategories.AddRange(
            new VehicleCategory { Id = 1, Name = "Light", MinWeight = 0.00m, MaxWeight = 500.00m, IconUrl = "/icons/light.svg" },
            new VehicleCategory { Id = 2, Name = "Medium", MinWeight = 500.01m, MaxWeight = 2500.00m, IconUrl = "/icons/medium.svg" },
            new VehicleCategory { Id = 3, Name = "Heavy", MinWeight = 2500.01m, MaxWeight = null, IconUrl = "/icons/heavy.svg" }
        );
        await context.SaveChangesAsync();
    }

    public static async Task SeedOwnerAndManufacturerAsync(VehicleDbContext context)
    {
        if (await context.Owners.AnyAsync() || await context.Manufacturers.AnyAsync())
            return; // already seeded via HasData

        context.Owners.Add(new Owner { Id = 1, Name = "Test Owner" });
        context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Test Maker" });
        await context.SaveChangesAsync();
    }
}