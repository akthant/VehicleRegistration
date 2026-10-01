using FluentAssertions;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Exceptions;
using VehicleRegistration.Application.Services;

namespace VehicleRegistration.IntegrationTests;

public class CategoryServiceTests
{
    private static CategoryService CreateService(string testName)
    {
        var context = TestDbContextFactory.Create(testName);
        return new CategoryService(context);
    }

    // ── ReplaceCategoriesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ReplaceCategories_ValidSet_Succeeds()
    {
        var service = CreateService(nameof(ReplaceCategories_ValidSet_Succeeds));
        var request = new CategorySetRequest(new()
        {
            new(null, "Light",  0.00m,    500.00m,  "/icons/light.svg"),
            new(null, "Medium", 500.01m,  2500.00m, "/icons/medium.svg"),
            new(null, "Heavy",  2500.01m, null,     "/icons/heavy.svg")
        });

        var result = await service.ReplaceCategoriesAsync(request);

        result.Should().HaveCount(3);
        result.Select(c => c.Name).Should().BeEquivalentTo("Light", "Medium", "Heavy");
    }

    [Fact]
    public async Task ReplaceCategories_WithGap_ThrowsValidationException()
    {
        var service = CreateService(nameof(ReplaceCategories_WithGap_ThrowsValidationException));
        var request = new CategorySetRequest(new()
        {
            new(null, "Light",  0.00m,   500.00m, "/icons/light.svg"),
            new(null, "Medium", 600.00m, null,    "/icons/medium.svg")  // gap: 500.01–599.99
        });

        var act = () => service.ReplaceCategoriesAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*validation*");
    }

    [Fact]
    public async Task ReplaceCategories_WithOverlap_ThrowsValidationException()
    {
        var service = CreateService(nameof(ReplaceCategories_WithOverlap_ThrowsValidationException));
        var request = new CategorySetRequest(new()
        {
            new(null, "Light",  0.00m,   600.00m, "/icons/light.svg"),  // ends at 600
            new(null, "Medium", 500.00m, null,    "/icons/medium.svg")   // starts at 500 — overlap
        });

        var act = () => service.ReplaceCategoriesAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ReplaceCategories_NotStartingAtZero_ThrowsValidationException()
    {
        var service = CreateService(nameof(ReplaceCategories_NotStartingAtZero_ThrowsValidationException));
        var request = new CategorySetRequest(new()
        {
            new(null, "Medium", 500.00m, null, "/icons/medium.svg")
        });

        var act = () => service.ReplaceCategoriesAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ReplaceCategories_ChangingBoundaryAtomically_NeverLeavesGap()
    {
        // This is the key scenario from the design doc:
        // Medium 500→2500, Heavy 2500→null  →  Medium 500→2000, Heavy 2000→null
        // Both must change together — if only one changed, there would be a gap/overlap.
        var context = TestDbContextFactory.Create(
            nameof(ReplaceCategories_ChangingBoundaryAtomically_NeverLeavesGap));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        var request = new CategorySetRequest(new()
        {
            new(1, "Light",  0.00m,    500.00m,  "/icons/light.svg"),
            new(2, "Medium", 500.01m,  2000.00m, "/icons/medium.svg"),  // boundary moved
            new(3, "Heavy",  2000.01m, null,      "/icons/heavy.svg")   // boundary moved
        });

        var result = await service.ReplaceCategoriesAsync(request);

        result.First(c => c.Name == "Medium").MaxWeight.Should().Be(2000.00m);
        result.First(c => c.Name == "Heavy").MinWeight.Should().Be(2000.01m);
    }

    // ── DeleteCategoryAsync (without absorb — must refuse) ────────────────────

    [Fact]
    public async Task DeleteCategory_WithoutAbsorb_ThrowsBusinessRuleException()
    {
        var context = TestDbContextFactory.Create(
            nameof(DeleteCategory_WithoutAbsorb_ThrowsBusinessRuleException));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        // Deleting the middle category creates a gap — must be refused
        var act = () => service.DeleteCategoryAsync(2);  // Medium

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*gap*");
    }

    [Fact]
    public async Task DeleteCategory_LastRemaining_ThrowsBusinessRuleException()
    {
        var context = TestDbContextFactory.Create(
            nameof(DeleteCategory_LastRemaining_ThrowsBusinessRuleException));

        context.VehicleCategories.Add(
            new() { Id = 1, Name = "Only", MinWeight = 0.00m, MaxWeight = null, IconUrl = "/icons/only.svg" });
        await context.SaveChangesAsync();
        var service = new CategoryService(context);

        var act = () => service.DeleteCategoryAsync(1);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*last remaining*");
    }

    // ── DeleteAndAbsorbAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAndAbsorb_IntoNext_ExpandsNextCategoryDownward()
    {
        var context = TestDbContextFactory.Create(
            nameof(DeleteAndAbsorb_IntoNext_ExpandsNextCategoryDownward));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        // Delete Light (id=1), Medium absorbs — now Medium starts at 0
        var result = await service.DeleteAndAbsorbAsync(1, AbsorbDirection.Next);

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "Medium" && c.MinWeight == 0.00m);
    }

    [Fact]
    public async Task DeleteAndAbsorb_IntoPrevious_ExpandsPreviousCategoryUpward()
    {
        var context = TestDbContextFactory.Create(
            nameof(DeleteAndAbsorb_IntoPrevious_ExpandsPreviousCategoryUpward));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        // Delete Heavy (id=3), Medium absorbs — now Medium has no upper limit
        var result = await service.DeleteAndAbsorbAsync(3, AbsorbDirection.Previous);

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "Medium" && c.MaxWeight == null);
    }

    [Fact]
    public async Task DeleteAndAbsorb_LightestAbsorbIntoPrevious_ThrowsBusinessRuleException()
    {
        var context = TestDbContextFactory.Create(
            nameof(DeleteAndAbsorb_LightestAbsorbIntoPrevious_ThrowsBusinessRuleException));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        // Light is the lightest — no previous category
        var act = () => service.DeleteAndAbsorbAsync(1, AbsorbDirection.Previous);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*no previous category*");
    }

    [Fact]
    public async Task DeleteAndAbsorb_HeaviestAbsorbIntoNext_ThrowsBusinessRuleException()
    {
        var context = TestDbContextFactory.Create(
            nameof(DeleteAndAbsorb_HeaviestAbsorbIntoNext_ThrowsBusinessRuleException));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        // Heavy is the heaviest — no next category
        var act = () => service.DeleteAndAbsorbAsync(3, AbsorbDirection.Next);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*no next category*");
    }

    // ── CreateCategoryAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_SplitsHostAtStart_HostMinWeightAdjusted()
    {
        var context = TestDbContextFactory.Create(
            nameof(CreateCategory_SplitsHostAtStart_HostMinWeightAdjusted));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        // Create "Micro" 0.00–99.99 inside Light (0–500): Micro takes the start, Light shrinks
        var request = new CreateCategoryRequest("Micro", 0.00m, 99.99m, "/icons/micro.svg");
        var created = await service.CreateCategoryAsync(request);

        created.Name.Should().Be("Micro");
        created.MinWeight.Should().Be(0.00m);
        created.MaxWeight.Should().Be(99.99m);

        // Light must now start at 100.00
        var light = (await service.GetAllCategoriesAsync()).First(c => c.Name == "Light");
        light.MinWeight.Should().Be(100.00m);
    }

    [Fact]
    public async Task CreateCategory_WithWeightOutsideAllRanges_ThrowsBusinessRuleException()
    {
        var context = TestDbContextFactory.Create(
            nameof(CreateCategory_WithWeightOutsideAllRanges_ThrowsBusinessRuleException));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        // Negative MinWeight is guaranteed to be outside all ranges
        var request = new CreateCategoryRequest("Ghost", -1.00m, 100.00m, "/icons/ghost.svg");
        var act = () => service.CreateCategoryAsync(request);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*No existing category contains*");
    }

    // ── UpdateCategoryAsync (name/icon only) ──────────────────────────────────

    [Fact]
    public async Task UpdateCategory_NameAndIcon_DoesNotAffectWeightRanges()
    {
        var context = TestDbContextFactory.Create(
            nameof(UpdateCategory_NameAndIcon_DoesNotAffectWeightRanges));
        await TestDbContextFactory.SeedStandardCategoriesAsync(context);
        var service = new CategoryService(context);

        var request = new UpdateCategoryRequest("Light Vehicle", "/icons/light-new.svg");
        var result = await service.UpdateCategoryAsync(1, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Light Vehicle");
        result.IconUrl.Should().Be("/icons/light-new.svg");
        result.MinWeight.Should().Be(0.00m);   // unchanged
        result.MaxWeight.Should().Be(500.00m); // unchanged
    }
}