using FluentAssertions;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Services;
using VehicleRegistration.Core.Entities;

namespace VehicleRegistration.IntegrationTests;

public class VehicleServiceTests
{
    // ── Category determination at boundary values ─────────────────────────────

    [Theory]
    [InlineData(0.00, "Light")]
    [InlineData(500.00, "Light")]   // upper boundary — inclusive
    [InlineData(500.01, "Medium")]  // lower boundary of Medium — inclusive
    [InlineData(2500.00, "Medium")]  // upper boundary of Medium — inclusive
    [InlineData(2500.01, "Heavy")]   // lower boundary of Heavy — inclusive
    [InlineData(9999.00, "Heavy")]   // well inside unbounded
    public async Task GetVehicleById_ReturnsCorrectCategory(
        decimal weight, string expectedCategory)
    {
        var context = TestDbContextFactory.Create(
            $"{nameof(GetVehicleById_ReturnsCorrectCategory)}_{weight}");
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        await TestDbContextFactory.SeedOwnerAndManufacturerAsync(context);
        var catService = new CategoryService(context);
        var svc = new VehicleService(context, catService);

        context.Vehicles.Add(new Vehicle
        {
            OwnerId = 1,
            ManufacturerId = 1,
            YearOfManufacture = 2020,
            Weight = weight
        });
        await context.SaveChangesAsync();

        var vehicle = await svc.GetVehicleByIdAsync(1);

        vehicle.Should().NotBeNull();
        vehicle!.CategoryName.Should().Be(expectedCategory,
            because: $"{weight} kg should be in '{expectedCategory}'");
    }

    // ── Category reflection after configuration change ────────────────────────

    [Fact]
    public async Task CategoryChanges_ImmediatelyReflectedOnVehicleList()
    {
        // Arrange: vehicle at 450 kg is "Light" under initial config (0–500)
        var context = TestDbContextFactory.Create(
            nameof(CategoryChanges_ImmediatelyReflectedOnVehicleList));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        await TestDbContextFactory.SeedOwnerAndManufacturerAsync(context);
        context.Vehicles.Add(new Vehicle
        {
            OwnerId = 1,
            ManufacturerId = 1,
            YearOfManufacture = 2020,
            Weight = 450.00m
        });
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);
        var vehicleService = new VehicleService(context, categoryService);

        var before = await vehicleService.GetAllVehiclesAsync(null, false);
        before[0].CategoryName.Should().Be("Light");

        // Act: change Light range to 0–400 (vehicle at 450 now falls in Medium)
        await categoryService.ReplaceCategoriesAsync(new CategorySetRequest(new()
        {
            new(1, "Light",  0.00m,    400.00m,  "/icons/light.svg"),
            new(2, "Medium", 400.01m,  2500.00m, "/icons/medium.svg"),
            new(3, "Heavy",  2500.01m, null,      "/icons/heavy.svg")
        }));

        // Assert: same vehicle now reports "Medium" — no re-insert needed
        var after = await vehicleService.GetAllVehiclesAsync(null, false);
        after[0].CategoryName.Should().Be("Medium",
            because: "category is computed at runtime from current category configuration");
    }

    // ── Sorting ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllVehicles_SortByWeight_Ascending_ReturnsInOrder()
    {
        var context = TestDbContextFactory.Create(
            nameof(GetAllVehicles_SortByWeight_Ascending_ReturnsInOrder));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        await TestDbContextFactory.SeedOwnerAndManufacturerAsync(context);
        context.Vehicles.AddRange(
            new Vehicle { OwnerId = 1, ManufacturerId = 1, YearOfManufacture = 2020, Weight = 3000.00m },
            new Vehicle { OwnerId = 1, ManufacturerId = 1, YearOfManufacture = 2021, Weight = 100.00m },
            new Vehicle { OwnerId = 1, ManufacturerId = 1, YearOfManufacture = 2022, Weight = 1500.00m }
        );
        await context.SaveChangesAsync();
        var svc = new VehicleService(context, new CategoryService(context));

        var result = await svc.GetAllVehiclesAsync("weight", false);

        result.Select(v => v.Weight).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAllVehicles_SortByOwnerName_Descending_ReturnsInOrder()
    {
        var context = TestDbContextFactory.Create(
            nameof(GetAllVehicles_SortByOwnerName_Descending_ReturnsInOrder));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        context.Owners.AddRange(
            new Owner { Id = 1, Name = "Alice" },
            new Owner { Id = 2, Name = "Charlie" },
            new Owner { Id = 3, Name = "Bob" }
        );
        context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Maker" });
        await context.SaveChangesAsync();
        context.Vehicles.AddRange(
            new Vehicle { OwnerId = 1, ManufacturerId = 1, YearOfManufacture = 2020, Weight = 100m },
            new Vehicle { OwnerId = 2, ManufacturerId = 1, YearOfManufacture = 2020, Weight = 200m },
            new Vehicle { OwnerId = 3, ManufacturerId = 1, YearOfManufacture = 2020, Weight = 300m }
        );
        await context.SaveChangesAsync();
        var svc = new VehicleService(context, new CategoryService(context));

        var result = await svc.GetAllVehiclesAsync("ownerName", true);

        result.Select(v => v.OwnerName).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetAllVehicles_UnknownSortBy_FallsBackToIdOrder()
    {
        // The service accepts unknown values gracefully (falls back to Id order).
        // The controller layer is responsible for rejecting them — tested in ApiIntegrationTests.
        var context = TestDbContextFactory.Create(
            nameof(GetAllVehicles_UnknownSortBy_FallsBackToIdOrder));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        await TestDbContextFactory.SeedOwnerAndManufacturerAsync(context);
        context.Vehicles.AddRange(
            new Vehicle { OwnerId = 1, ManufacturerId = 1, YearOfManufacture = 2020, Weight = 100m },
            new Vehicle { OwnerId = 1, ManufacturerId = 1, YearOfManufacture = 2021, Weight = 200m }
        );
        await context.SaveChangesAsync();
        var svc = new VehicleService(context, new CategoryService(context));

        var result = await svc.GetAllVehiclesAsync("notAColumn", false);

        result.Should().HaveCount(2);
        result.Select(v => v.Id).Should().BeInAscendingOrder();
    }
}