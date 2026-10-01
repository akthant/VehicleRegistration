using System;
using System.Collections.Generic;
using System.Text;

namespace VehicleRegistration.Core.Rules;

public sealed record CategoryRange(string Name, decimal MinWeight, decimal? MaxWeight);

public static class CategoryRangeRules
{
    public const decimal Step = 0.01m; // boundaries are inclusive, 2 d.p.

    public static IReadOnlyList<string> Validate(IEnumerable<CategoryRange> categories)
    {
        var errors = new List<string>();
        var ordered = categories.OrderBy(c => c.MinWeight).ToList();

        if (ordered.Count == 0)
        {
            errors.Add("At least one category is required.");
            return errors;
        }

        // ---- Per-row rules ----
        foreach (var c in ordered)
        {
            if (string.IsNullOrWhiteSpace(c.Name))
                errors.Add("Every category needs a name.");
            if (c.MinWeight < 0)
                errors.Add($"'{c.Name}' cannot start below 0.00 kg.");
            if (c.MaxWeight is decimal max && max <= c.MinWeight)
                errors.Add($"'{c.Name}' must end above its start weight ({c.MinWeight:0.00} kg).");
        }

        var duplicateNames = ordered
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var name in duplicateNames)
            errors.Add($"Category name '{name}' is used more than once.");

        // ---- Whole-configuration rules ----
        if (ordered[0].MinWeight != 0m)
            errors.Add($"The lightest category ('{ordered[0].Name}') must start at 0.00 kg.");

        var unboundedCount = ordered.Count(c => c.MaxWeight is null);
        if (unboundedCount == 0)
            errors.Add($"The heaviest category ('{ordered[^1].Name}') must have no upper limit.");
        else if (unboundedCount > 1)
            errors.Add("Only one category may have no upper limit.");

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var current = ordered[i];
            var next = ordered[i + 1];

            if (current.MaxWeight is not decimal max)
            {
                errors.Add($"'{current.Name}' has no upper limit, so no category can follow it " +
                           $"('{next.Name}' starts at {next.MinWeight:0.00} kg).");
                continue;
            }

            var expectedNextMin = max + Step;
            if (next.MinWeight > expectedNextMin)
                errors.Add($"Gap: weights {expectedNextMin:0.00} kg to {next.MinWeight - Step:0.00} kg have no category " +
                           $"(between '{current.Name}' and '{next.Name}').");
            else if (next.MinWeight < expectedNextMin)
                errors.Add($"Overlap: '{current.Name}' ends at {max:0.00} kg but '{next.Name}' starts at " +
                           $"{next.MinWeight:0.00} kg. '{next.Name}' should start at {expectedNextMin:0.00} kg.");
        }

        return errors;
    }
}



