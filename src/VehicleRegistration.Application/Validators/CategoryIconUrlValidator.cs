namespace VehicleRegistration.Application.Validators;

public static class CategoryIconUrlValidator
{
    public static bool IsValid(string? iconUrl)
    {
        return !string.IsNullOrWhiteSpace(iconUrl)
            && iconUrl.StartsWith("/icons/", StringComparison.Ordinal)
            && !iconUrl.Contains("..", StringComparison.Ordinal)
            && Uri.TryCreate(iconUrl, UriKind.Relative, out _);
    }
}