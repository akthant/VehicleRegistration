using FluentAssertions;
using VehicleRegistration.Core.Rules;

namespace VehicleRegistration.UnitTests;

public class CategoryRangeRulesTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CategoryRange R(string name, decimal min, decimal? max)
        => new(name, min, max);

    private static IReadOnlyList<string> Validate(params CategoryRange[] ranges)
        => CategoryRangeRules.Validate(ranges);

    // ── Valid configurations ──────────────────────────────────────────────────

    [Fact]
    public void Valid_ThreeCategories_ReturnsNoErrors()
    {
        var errors = Validate(
            R("Light", 0.00m, 500.00m),
            R("Medium", 500.01m, 2500.00m),
            R("Heavy", 2500.01m, null));

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Valid_SingleUnboundedCategory_ReturnsNoErrors()
    {
        var errors = Validate(R("All", 0.00m, null));

        errors.Should().BeEmpty();
    }

    // ── Empty input ───────────────────────────────────────────────────────────

    [Fact]
    public void Empty_ReturnsAtLeastOneRequired()
    {
        var errors = CategoryRangeRules.Validate(Array.Empty<CategoryRange>());

        errors.Should().ContainSingle()
            .Which.Should().Contain("At least one category is required.");
    }

    // ── First category must start at 0 ────────────────────────────────────────

    [Fact]
    public void FirstCategory_DoesNotStartAtZero_ReturnsError()
    {
        var errors = Validate(
            R("Light", 0.01m, 500.00m),
            R("Medium", 500.01m, null));

        errors.Should().Contain(e => e.Contains("must start at 0.00 kg"));
    }

    // ── Unbounded category ────────────────────────────────────────────────────

    [Fact]
    public void NoUnboundedCategory_ReturnsError()
    {
        var errors = Validate(
            R("Light", 0.00m, 500.00m),
            R("Medium", 500.01m, 2500.00m));

        errors.Should().Contain(e => e.Contains("no upper limit"));
    }

    [Fact]
    public void MultipleUnboundedCategories_ReturnsError()
    {
        var errors = Validate(
            R("Light", 0.00m, 500.00m),
            R("Medium", 500.01m, null),
            R("Heavy", 500.01m, null));

        errors.Should().Contain(e => e.Contains("Only one category may have no upper limit"));
    }

    // ── Gap detection ─────────────────────────────────────────────────────────

    [Fact]
    public void Gap_BetweenCategories_ReturnsError()
    {
        // Light ends at 500, Medium starts at 600 → gap 500.01–599.99
        var errors = Validate(
            R("Light", 0.00m, 500.00m),
            R("Medium", 600.00m, null));

        errors.Should().Contain(e => e.Contains("Gap"));
    }

    [Fact]
    public void Gap_AtExactBoundary_IsNotAGap()
    {
        // Light ends at 500.00, Medium starts at 500.01 — valid
        var errors = Validate(
            R("Light", 0.00m, 500.00m),
            R("Heavy", 500.01m, null));

        errors.Should().BeEmpty();
    }

    // ── Overlap detection ─────────────────────────────────────────────────────

    [Fact]
    public void Overlap_BetweenCategories_ReturnsError()
    {
        // Light ends at 600, Medium starts at 500 — overlap
        var errors = Validate(
            R("Light", 0.00m, 600.00m),
            R("Medium", 500.00m, null));

        errors.Should().Contain(e => e.Contains("Overlap"));
    }

    [Fact]
    public void Overlap_NextStartsOneStepBeforeExpected_ReturnsError()
    {
        // Light ends at 500, Medium starts at 500 instead of 500.01
        var errors = Validate(
            R("Light", 0.00m, 500.00m),
            R("Medium", 500.00m, null));

        errors.Should().Contain(e => e.Contains("Overlap"));
    }

    // ── Boundary values (inclusive) ───────────────────────────────────────────

    [Theory]
    [InlineData(0.00, "Light")]   // minimum of first category
    [InlineData(500.00, "Light")]   // upper boundary — inclusive
    [InlineData(500.01, "Medium")]  // lower boundary of next — inclusive
    [InlineData(2500.00, "Medium")]  // upper boundary of Medium — inclusive
    [InlineData(2500.01, "Heavy")]   // lower boundary of Heavy — inclusive
    [InlineData(9999.99, "Heavy")]   // well inside unbounded category
    public void BoundaryValues_AreAssignedCorrectCategory(
        decimal weight, string expectedCategory)
    {
        var config = new[]
        {
            R("Light",  0.00m,    500.00m),
            R("Medium", 500.01m,  2500.00m),
            R("Heavy",  2500.01m, null)
        };

        // Sanity: configuration itself is valid
        Validate(config).Should().BeEmpty(
            because: $"the standard config must be valid to test weight {weight}");

        // Verify the expected category contains the weight
        var matchingCategory = config.FirstOrDefault(c =>
            c.MinWeight <= weight && (c.MaxWeight is null || c.MaxWeight >= weight));

        matchingCategory.Should().NotBeNull(
            because: $"{weight} kg must belong to a category");
        matchingCategory!.Name.Should().Be(expectedCategory,
            because: $"{weight} kg should be '{expectedCategory}'");
    }

    // ── Per-row validation ────────────────────────────────────────────────────

    [Fact]
    public void MaxWeight_LessThanMinWeight_ReturnsError()
    {
        var errors = Validate(R("Bad", 0.00m, -1.00m));

        errors.Should().Contain(e => e.Contains("must end above its start weight"));
    }

    [Fact]
    public void NegativeMinWeight_ReturnsError()
    {
        var errors = Validate(R("Bad", -1.00m, null));

        errors.Should().Contain(e => e.Contains("cannot start below 0.00 kg"));
    }

    [Fact]
    public void DuplicateName_ReturnsError()
    {
        var errors = Validate(
            R("Light", 0.00m, 500.00m),
            R("Light", 500.01m, null));

        errors.Should().Contain(e => e.Contains("used more than once"));
    }

    [Fact]
    public void EmptyName_ReturnsError()
    {
        var errors = Validate(R("", 0.00m, null));

        errors.Should().Contain(e => e.Contains("needs a name"));
    }
}