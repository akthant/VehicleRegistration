using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Infrastructure.Data;

namespace VehicleRegistration.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = "ApiTestDb_" + Guid.NewGuid();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace SQL Server with InMemory for tests
                services.RemoveAll<IDbContextOptionsConfiguration<VehicleDbContext>>();
                services.RemoveAll<DbContextOptions<VehicleDbContext>>();

                services.AddDbContext<VehicleDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName)
                        .ConfigureWarnings(warnings =>
                            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            });
        }).CreateClient();
    }

    // ── Categories ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PutCategories_WithGap_Returns400()
    {
        var body = new
        {
            categories = new[]
            {
                new { name = "Light",  minWeight = 0.00m,   maxWeight = (decimal?)500.00m, iconUrl = "/icons/light.svg" },
                new { name = "Medium", minWeight = 600.00m, maxWeight = (decimal?)null,    iconUrl = "/icons/medium.svg" }
                // gap: 500.01–599.99
            }
        };

        var response = await _client.PutAsJsonAsync("/api/categories", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutCategories_ValidSet_Returns200AndCategories()
    {
        var body = new
        {
            categories = new[]
            {
                new { name = "Light",  minWeight = 0.00m,    maxWeight = (decimal?)500.00m,  iconUrl = "/icons/light.svg" },
                new { name = "Medium", minWeight = 500.01m,  maxWeight = (decimal?)2500.00m, iconUrl = "/icons/medium.svg" },
                new { name = "Heavy",  minWeight = 2500.01m, maxWeight = (decimal?)null,     iconUrl = "/icons/heavy.svg" }
            }
        };

        var response = await _client.PutAsJsonAsync("/api/categories", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryResponse>>>();
        result!.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteCategory_WithoutAbsorbParam_Returns400WithExplanation()
    {
        // Seed categories first
        await _client.PutAsJsonAsync("/api/categories", new
        {
            categories = new[]
            {
                new { name = "Light",  minWeight = 0.00m,    maxWeight = (decimal?)500.00m,  iconUrl = "/icons/light.svg" },
                new { name = "Medium", minWeight = 500.01m,  maxWeight = (decimal?)2500.00m, iconUrl = "/icons/medium.svg" },
                new { name = "Heavy",  minWeight = 2500.01m, maxWeight = (decimal?)null,     iconUrl = "/icons/heavy.svg" }
            }
        });

        var categories = await _client.GetFromJsonAsync<ApiResponse<List<CategoryResponse>>>("/api/categories");
        var mediumId = categories!.Data.First(c => c.Name == "Medium").Id;

        // Delete without absorbInto — must be refused with an explanation
        var response = await _client.DeleteAsync($"/api/categories/{mediumId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("gap");
    }

    // ── Vehicles — sort field allowlist ───────────────────────────────────────

    [Fact]
    public async Task GetVehicles_InvalidSortBy_Returns400()
    {
        var response = await _client.GetAsync("/api/vehicles?sortBy=dropTable");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid sortBy");
    }

    [Theory]
    [InlineData("ownerName")]
    [InlineData("manufacturer")]
    [InlineData("year")]
    [InlineData("weight")]
    public async Task GetVehicles_ApprovedSortFields_Return200(string sortBy)
    {
        var response = await _client.GetAsync($"/api/vehicles?sortBy={sortBy}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetVehicles_NoSortBy_Returns200()
    {
        var response = await _client.GetAsync("/api/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Vehicles — create validation ─────────────────────────────────────────

    [Fact]
    public async Task PostVehicle_NegativeWeight_Returns400()
    {
        var body = new
        {
            ownerId = 1,
            manufacturerId = 1,
            yearOfManufacture = 2020,
            weight = -1.00
        };

        var response = await _client.PostAsJsonAsync("/api/vehicles", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostVehicle_InvalidYear_Returns400()
    {
        var body = new
        {
            ownerId = 1,
            manufacturerId = 1,
            yearOfManufacture = 1800,
            weight = 1000.00
        };

        var response = await _client.PostAsJsonAsync("/api/vehicles", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}