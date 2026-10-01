using FluentValidation;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Core.Rules;
namespace VehicleRegistration.Application.Validators;
using System.Text.RegularExpressions;

public class CategorySetValidator : AbstractValidator<CategorySetRequest>
{
    public CategorySetValidator()
    {
        RuleFor(x => x.Categories)
            .NotEmpty()
            .WithMessage("At least one category is required.");

        RuleFor(x => x.Categories)
            .Must(HaveFirstCategoryStartAtZero)
            .WithMessage("First category must start at 0.00 kg.");

        RuleFor(x => x.Categories)
            .Must(HaveExactlyOneUnboundedCategory)
            .WithMessage("Exactly one category must have no upper limit (MaxWeight = null).");

        RuleFor(x => x.Categories)
            .Must(HaveNoGapsOrOverlaps)
            .WithMessage("Categories must have no gaps or overlaps. Next MinWeight must equal previous MaxWeight + 0.01.");

        RuleForEach(x => x.Categories).SetValidator(new CategoryItemValidator());
    }

    private bool HaveFirstCategoryStartAtZero(List<CategoryItemRequest> categories)
    {
        if (!categories.Any()) return false;
        var sorted = categories.OrderBy(c => c.MinWeight).ToList();
        return sorted.First().MinWeight == 0.00m;
    }

    private bool HaveExactlyOneUnboundedCategory(List<CategoryItemRequest> categories)
    {
        return categories.Count(c => c.MaxWeight == null) == 1;
    }

    private bool HaveNoGapsOrOverlaps(List<CategoryItemRequest> categories)
    {
        if (categories.Count < 2) return true;

        var sorted = categories.OrderBy(c => c.MinWeight).ToList();

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            if (current.MaxWeight == null && i < sorted.Count - 1)
                return false;

            var expectedNextMin = current.MaxWeight!.Value + 0.01m;
            if (next.MinWeight != expectedNextMin)
                return false;
        }

        return true;
    }
}

public class CategoryItemValidator : AbstractValidator<CategoryItemRequest>
{
    public CategoryItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.MinWeight)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MinWeight must be non-negative.")
            .PrecisionScale(10, 2, false)
            .WithMessage("MinWeight must have maximum 2 decimal places.");

        RuleFor(x => x.MaxWeight)
            .GreaterThan(x => x.MinWeight)
            .When(x => x.MaxWeight.HasValue)
            .WithMessage("MaxWeight must be greater than MinWeight.")
            .PrecisionScale(10, 2, false)
            .When(x => x.MaxWeight.HasValue)
            .WithMessage("MaxWeight must have maximum 2 decimal places.");

        RuleFor(x => x.IconUrl)
             .NotEmpty()
             .WithMessage("Icon URL is required.")
             .MaximumLength(500)
             .WithMessage("Icon URL must not exceed 500 characters.")
             .Must(BeAllowedIconPath)
             .WithMessage("Icon URL must refer to a bundled icon or an uploaded PNG, JPEG, or WebP image.");
    }
    private static bool BeAllowedIconPath(string? iconUrl)
    {
        if (string.IsNullOrWhiteSpace(iconUrl))
            return false;

        return iconUrl is "/icons/light.svg" or "/icons/medium.svg" or "/icons/heavy.svg"
            || Regex.IsMatch(
                iconUrl,
                @"\A/icons/(?:[a-f0-9]{32}|[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12})\.(?:png|jpg|webp)\z",
                RegexOptions.IgnoreCase);
    }
}

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MinWeight).GreaterThanOrEqualTo(0).PrecisionScale(10, 2, false);
        When(x => x.MaxWeight.HasValue, () =>
            RuleFor(x => x.MaxWeight).GreaterThan(x => x.MinWeight).PrecisionScale(10, 2, false));
        RuleFor(x => x.IconUrl).NotEmpty().MaximumLength(500);
    }
}