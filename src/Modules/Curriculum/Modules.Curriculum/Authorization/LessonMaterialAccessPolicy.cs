using FSH.Modules.Files.Contracts;

namespace FSH.Modules.Curriculum.Authorization;

/// <summary>
/// IFileAccessPolicy for lesson material files (OwnerType=LessonMaterial, OwnerId=LessonId —
/// the file belongs to the lesson it's attached to, mirroring Catalog's
/// <c>ProductFileAccessPolicy</c> which keys on the product, not the individual image row).
///
/// - Attach: any authenticated user. The durable gate is the lesson-materials endpoint's own
///   permission check (<c>LessonMaterials.Manage</c>).
/// - Read: open. Whether a *student* may see a given material is decided by
///   <c>LessonMaterial.VisibleToStudents</c> on the Curriculum side, not by this policy — the
///   file layer has no notion of that flag.
/// - Delete: uploader-only, same convention as <c>ProductFileAccessPolicy</c>.
/// </summary>
public sealed class LessonMaterialAccessPolicy : IFileAccessPolicy
{
    public string OwnerType => "LessonMaterial";

    public Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(currentUserId));

    public Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(
            !string.IsNullOrEmpty(currentUserId)
            && string.Equals(currentUserId, context.CreatedByUserId, StringComparison.Ordinal));
    }
}
