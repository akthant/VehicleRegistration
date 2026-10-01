using FluentAssertions;
using FluentValidation.TestHelper;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Validators;

namespace VehicleRegistration.UnitTests;

public class CreateVehicleValidatorTests
{
    private readonly CreateVehicleValidator _validator = new();

    private static CreateVehicleRequest Valid() => new(
        OwnerId: 1,
        ManufacturerId: 1,
        YearOfManufacture: 2020,
        Weight: 1000.00m);

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    // ── Owner ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OwnerId_ZeroOrNegative_FailsValidation(int ownerId)
    {
        var result = _validator.TestValidate(Valid() with { OwnerId = ownerId });
        result.ShouldHaveValidationErrorFor(x => x.OwnerId);
    }

    // ── Manufacturer ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ManufacturerId_ZeroOrNegative_FailsValidation(int manufacturerId)
    {
        var result = _validator.TestValidate(Valid() with { ManufacturerId = manufacturerId });
        result.ShouldHaveValidationErrorFor(x => x.ManufacturerId);
    }

    // ── Year ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Year_Before1886_FailsValidation()
    {
        var result = _validator.TestValidate(Valid() with { YearOfManufacture = 1885 });
        result.ShouldHaveValidationErrorFor(x => x.YearOfManufacture);
    }

    [Fact]
    public void Year_TwoYearsInFuture_FailsValidation()
    {
        var result = _validator.TestValidate(
            Valid() with { YearOfManufacture = DateTime.Now.Year + 2 });
        result.ShouldHaveValidationErrorFor(x => x.YearOfManufacture);
    }

    [Fact]
    public void Year_CurrentYearPlusOne_PassesValidation()
    {
        var result = _validator.TestValidate(
            Valid() with { YearOfManufacture = DateTime.Now.Year + 1 });
        result.ShouldNotHaveValidationErrorFor(x => x.YearOfManufacture);
    }

    // ── Weight ────────────────────────────────────────────────────────────────

    [Fact]
    public void Weight_Negative_FailsValidation()
    {
        var result = _validator.TestValidate(Valid() with { Weight = -0.01m });
        result.ShouldHaveValidationErrorFor(x => x.Weight);
    }

    [Fact]
    public void Weight_Zero_PassesValidation()
    {
        var result = _validator.TestValidate(Valid() with { Weight = 0.00m });
        result.ShouldNotHaveValidationErrorFor(x => x.Weight);
    }

    [Fact]
    public void Weight_ThreeDecimalPlaces_FailsValidation()
    {
        var result = _validator.TestValidate(Valid() with { Weight = 100.001m });
        result.ShouldHaveValidationErrorFor(x => x.Weight);
    }

    [Fact]
    public void Weight_TwoDecimalPlaces_PassesValidation()
    {
        var result = _validator.TestValidate(Valid() with { Weight = 100.01m });
        result.ShouldNotHaveValidationErrorFor(x => x.Weight);
    }
}