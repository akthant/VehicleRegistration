using FluentValidation;
using VehicleRegistration.Application.DTOs;

namespace VehicleRegistration.Application.Validators;

public class CreateOwnerValidator : AbstractValidator<CreateOwnerRequest>
{
    public CreateOwnerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Owner name is required.")
            .MaximumLength(100).WithMessage("Owner name must not exceed 100 characters.");
    }
}