namespace FSH.Modules.Files.Services;

/// <summary>
/// Pure resolution of the "which category may this owner type upload through" rule declared by
/// <see cref="FileCategoryOptions.OwnerTypes"/>. Kept separate from
/// <c>RequestUploadUrlCommandHandler</c> so the binding logic is unit-testable without a DbContext
/// or storage provider.
/// </summary>
public static class FileCategoryPolicy
{
    public enum Outcome
    {
        Allowed,

        /// <summary>The chosen category is bound to other owner types and rejects this one.</summary>
        CategoryNotForOwnerType,

        /// <summary>This owner type is bound to a category (or categories) and this is not one of them.</summary>
        OwnerTypeRequiresBoundCategory,
    }

    /// <param name="categories">The configured <see cref="FilesOptions.Categories"/> map.</param>
    /// <param name="chosenCategory">The category name the caller asked to upload through. Assumed to already exist in <paramref name="categories"/>.</param>
    /// <param name="ownerType">The owner type the caller is attaching the file to.</param>
    /// <param name="requiredCategories">On a non-Allowed outcome, the categories the owner type is actually bound to (may be empty).</param>
    public static Outcome Check(
        IReadOnlyDictionary<string, FileCategoryOptions> categories,
        string chosenCategory,
        string ownerType,
        out IReadOnlyList<string> requiredCategories)
    {
        ArgumentNullException.ThrowIfNull(categories);

        requiredCategories = categories
            .Where(kv => kv.Value.OwnerTypes.Contains(ownerType, StringComparer.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (categories.TryGetValue(chosenCategory, out var chosen)
            && chosen.OwnerTypes.Count > 0
            && !chosen.OwnerTypes.Contains(ownerType, StringComparer.OrdinalIgnoreCase))
        {
            return Outcome.CategoryNotForOwnerType;
        }

        if (requiredCategories.Count > 0
            && !requiredCategories.Contains(chosenCategory, StringComparer.OrdinalIgnoreCase))
        {
            return Outcome.OwnerTypeRequiresBoundCategory;
        }

        return Outcome.Allowed;
    }
}
