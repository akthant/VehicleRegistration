using FluentValidation;
using VehicleRegistration.Application.DTOs;
namespace VehicleRegistration.Application.Validators;

public class CreateVehicleValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.OwnerId)
            .GreaterThan(0)
            .WithMessage("Owner is required.");

        RuleFor(x => x.ManufacturerId)
            .GreaterThan(0)
            .WithMessage("Manufacturer is required.");

        RuleFor(x => x.YearOfManufacture)
            .InclusiveBetween(1886, DateTime.Now.Year + 1)
            .WithMessage($"Year must be between 1886 and {DateTime.Now.Year + 1}.");

        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Weight must be a non-negative value.")
            .PrecisionScale(10, 2, false)
            .WithMessage("Weight must have maximum 2 decimal places.");
    }
}

public class UpdateVehicleValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleValidator()
    {
        RuleFor(x => x.OwnerId)
            .GreaterThan(0)
            .WithMessage("Owner is required.");

        RuleFor(x => x.ManufacturerId)
            .GreaterThan(0)
            .WithMessage("Manufacturer is required.");

        RuleFor(x => x.YearOfManufacture)
            .InclusiveBetween(1886, DateTime.Now.Year + 1)
            .WithMessage($"Year must be between 1886 and {DateTime.Now.Year + 1}.");

        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Weight must be a non-negative value.")
            .PrecisionScale(10, 2, false)
            .WithMessage("Weight must have maximum 2 decimal places.");
    }
}