using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Exceptions;
using VehicleRegistration.Application.Validators;
using VehicleRegistration.Core.Entities;
using VehicleRegistration.Core.Rules;
using VehicleRegistration.Infrastructure.Data;

namespace VehicleRegistration.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly VehicleDbContext _context;

    public CategoryService(VehicleDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryResponse>> GetAllCategoriesAsync()
    {
        return await _context.VehicleCategories
            .OrderBy(c => c.MinWeight)
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.MinWeight,
                c.MaxWeight,
                c.IconUrl))
            .ToListAsync();
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await _context.VehicleCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct); ;
        if (category == null) return null;

        return category is null ? null : new CategoryResponse(
            category.Id,
            category.Name,
            category.MinWeight,
            category.MaxWeight,
            category.IconUrl);
    }

    public async Task<CategoryResponse?> GetCategoryForWeightAsync(decimal weight, CancellationToken ct = default)
    {
        var category = await _context.VehicleCategories.AsNoTracking()
          .FirstOrDefaultAsync(x => x.MinWeight <= weight && (x.MaxWeight == null || x.MaxWeight >= weight), ct);
        
        return category is null ? null : new CategoryResponse(
          category.Id,
          category.Name,
          category.MinWeight,
          category.MaxWeight,
          category.IconUrl);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var saved = await CommitAsync(current =>
        {
            var targetCategory = current.OrderBy(c => c.MinWeight).Select(ToItem).ToList();
            var index = targetCategory.FindIndex(c => Contains(c, request.MinWeight));
            if (index < 0)
                throw new BusinessRuleException($"No existing category contains {request.MinWeight:0.00} kg.");

            var host = targetCategory[index];
            var fitsInsideHost = request.MaxWeight is null
                ? host.MaxWeight is null
                : host.MaxWeight is null || request.MaxWeight <= host.MaxWeight;
            if (!fitsInsideHost)
                throw new BusinessRuleException(
                    $"The new range must fit inside a single existing category. '{host.Name}' covers {Describe(host)}.");

            var startsAtHostStart = request.MinWeight == host.MinWeight;
            var endsAtHostEnd = request.MaxWeight == host.MaxWeight;
            var newItem = new CategoryItemRequest(null, request.Name, request.MinWeight, request.MaxWeight, request.IconUrl);

            if (startsAtHostStart && endsAtHostEnd)
                throw new BusinessRuleException(
                    $"This range is exactly '{host.Name}'. Rename '{host.Name}' or delete it instead.");

            if (startsAtHostStart)
            {
                // new category takes the lower part of the host
                targetCategory[index] = host with { MinWeight = request.MaxWeight!.Value + CategoryRangeRules.Step };
                targetCategory.Insert(index, newItem);
            }
            else if (endsAtHostEnd)
            {
                // new category takes the upper part of the host
                targetCategory[index] = host with { MaxWeight = request.MinWeight - CategoryRangeRules.Step };
                targetCategory.Insert(index + 1, newItem);
            }
            else
            {
                throw new BusinessRuleException(
                    $"This would split '{host.Name}' into two separate ranges. Start the new category at " +
                    $"{host.MinWeight:0.00} kg or end it at the end of '{host.Name}', or edit the full configuration.");
            }

            return targetCategory;
        }, ct);

        // Return only the newly created category (the one with no original Id)
        var created = saved.First(c => c.Name == request.Name
                                    && c.MinWeight == request.MinWeight
                                    && c.MaxWeight == request.MaxWeight);
        return created;
    }

    public async Task<CategoryResponse?> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var saved = await CommitAsync(current =>
        {
            var targetCategory = current.OrderBy(c => c.MinWeight).Select(ToItem).ToList();
            var index = targetCategory.FindIndex(c => c.Id == id);
            if (index < 0)
                throw new NotFoundException("Category", id);

            if (!CategoryIconUrlValidator.IsValid(request.IconUrl))
            {
                throw new ArgumentException(
                    "IconUrl must be a relative path under /icons/.");
            }

            var target = targetCategory[index];
            targetCategory[index] = target with
            {
                Name = request.Name,
                IconUrl = request.IconUrl
                // MinWeight / MaxWeight are intentionally NOT changed here.
                // Range changes require saving the full configuration.
            };

            return targetCategory;
        }, ct);

        return saved.FirstOrDefault(c => c.Id == id);
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        // Refuse deletion without an absorb direction — callers should use DeleteAndAbsorbAsync.
        // This overload stays in the interface for compatibility but always explains why it was refused.
        var current = await _context.VehicleCategories.AsNoTracking()
            .OrderBy(c => c.MinWeight).ToListAsync(ct);

        if (current.All(c => c.Id != id))
            return false;

        if (current.Count == 1)
            throw new BusinessRuleException("The last remaining category cannot be deleted.");

        var target = current.First(c => c.Id == id);
        var previous = current.LastOrDefault(c => c.MinWeight < target.MinWeight);
        var next = current.FirstOrDefault(c => c.MinWeight > target.MinWeight);

        var neighbourNames = string.Join(" or ", new[] { previous?.Name, next?.Name }
            .Where(n => n != null)
            .Select(n => $"'{n}'"));

        throw new BusinessRuleException(
            $"'{target.Name}' ({Describe(ToItem(target))}) cannot be deleted because deleting it would create a gap. " +
            $"Adjust the neighbouring category ranges first, or delete it and let {neighbourNames} absorb its range.");
    }

    public async Task<List<CategoryResponse>> DeleteAndAbsorbAsync(int id, AbsorbDirection absorbInto, CancellationToken ct = default)
    {
        return await CommitAsync(current =>
        {
            var targetCategory = current.OrderBy(c => c.MinWeight).Select(ToItem).ToList();
            var index = targetCategory.FindIndex(c => c.Id == id);
            if (index < 0)
                throw new NotFoundException("Category", id);

            if (targetCategory.Count == 1)
                throw new BusinessRuleException("The last remaining category cannot be deleted.");

            var target = targetCategory[index];
            var previous = index > 0 ? targetCategory[index - 1] : null;
            var next = index < targetCategory.Count - 1 ? targetCategory[index + 1] : null;

            if (absorbInto == AbsorbDirection.Previous)
            {
                if (previous is null)
                    throw new BusinessRuleException(
                        $"'{target.Name}' is the lightest category; it has no previous category to absorb its range.");

                targetCategory[index - 1] = previous with { MaxWeight = target.MaxWeight };
            }
            else
            {
                if (next is null)
                    throw new BusinessRuleException(
                        $"'{target.Name}' is the heaviest category; it has no next category to absorb its range.");

                targetCategory[index + 1] = next with { MinWeight = target.MinWeight };
            }

            targetCategory.RemoveAt(index);
            return targetCategory;
        }, ct);
    }

    public async Task<List<CategoryResponse>> ReplaceCategoriesAsync(CategorySetRequest request)
    {
        return await CommitAsync(_ => request.Categories, CancellationToken.None);
    }

    // ===================== Single transactional pipeline =====================

    private async Task<List<CategoryResponse>> CommitAsync(
        Func<IReadOnlyList<VehicleCategory>, IReadOnlyList<CategoryItemRequest>> buildTarget,
        CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        // 1. Load current state inside the transaction
        var current = await _context.VehicleCategories.ToListAsync(ct);

        // 2. Build the complete targetCategory configuration
        var targetCategory = buildTarget(current);

        // 3. Validate the complete targetCategory configuration before touching the database
        var ranges = targetCategory.Select(d => new CategoryRange(d.Name, d.MinWeight, d.MaxWeight));
        var errors = CategoryRangeRules.Validate(ranges);
        if (errors.Count > 0)
        {
            throw new ValidationException(
                new Dictionary<string, string[]> { ["Categories"] = errors.ToArray() });
        }

        // 4. Apply differences (preserve IDs where possible)
        ApplyDiff(current, targetCategory);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessRuleException(
                "Category names must be unique. If you are swapping names, save in two steps.");
        }

        await transaction.CommitAsync(ct);

        // 5. Return the saved rows in order
        return await _context.VehicleCategories
            .AsNoTracking()
            .OrderBy(c => c.MinWeight)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.MinWeight, c.MaxWeight, c.IconUrl))
            .ToListAsync(ct);
    }

    private void ApplyDiff(IReadOnlyList<VehicleCategory> current, IReadOnlyList<CategoryItemRequest> targetCategory)
    {
        var byId = current.ToDictionary(c => c.Id);
        var keptIds = targetCategory.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();

        // Remove categories not present in the targetCategory set
        _context.VehicleCategories.RemoveRange(current.Where(c => !keptIds.Contains(c.Id)));

        foreach (var item in targetCategory)
        {
            VehicleCategory entity;

            if (item.Id is int existingId)
            {
                // Update existing row
                entity = byId[existingId];
            }
            else
            {
                // Insert new row
                entity = new VehicleCategory();
                _context.VehicleCategories.Add(entity);
            }

            entity.Name = item.Name.Trim();
            entity.MinWeight = item.MinWeight;
            entity.MaxWeight = item.MaxWeight;
            entity.IconUrl = item.IconUrl.Trim();
        }
    }

    // ===================== Helpers =====================

    private static CategoryItemRequest ToItem(VehicleCategory c)
    {
        return new CategoryItemRequest(c.Id, c.Name, c.MinWeight, c.MaxWeight, c.IconUrl);
    }

    private static bool Contains(CategoryItemRequest c, decimal weight)
    {
        return c.MinWeight <= weight && (c.MaxWeight is null || c.MaxWeight >= weight);
    }

    private static string Describe(CategoryItemRequest c)
    {
        return c.MaxWeight is decimal max
            ? $"{c.MinWeight:0.00}–{max:0.00} kg"
            : $"{c.MinWeight:0.00} kg and above";
    }
}