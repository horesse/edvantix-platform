using FSH.Modules.Files.Contracts;
using FSH.Modules.Payments.Contracts;

namespace FSH.Modules.Curriculum.Authorization;

/// <summary>
/// IFileAccessPolicy for lesson material files (OwnerType=LessonMaterial, OwnerId=LessonId —
/// the file belongs to the lesson it's attached to, mirroring Catalog's
/// <c>ProductFileAccessPolicy</c> which keys on the product, not the individual image row).
///
/// - Attach: any authenticated user. The durable gate is the lesson-materials endpoint's own
///   permission check (<c>LessonMaterials.Manage</c>).
/// - Read: open, EXCEPT when the caller is a student/guardian blocked by the EDX-015
///   materials-on-debt rule (<see cref="IMaterialsAccessService"/> — tenant flag OFF by default,
///   so this is a no-op read of cached tenant settings for most schools). Whether a *student* may
///   see a given material's row is still decided by <c>LessonMaterial.VisibleToStudents</c> on the
///   Curriculum side; the file layer has no notion of that flag.
/// - Delete: uploader-only, same convention as <c>ProductFileAccessPolicy</c>.
/// </summary>
public sealed class LessonMaterialAccessPolicy(IMaterialsAccessService materialsAccess) : IFileAccessPolicy
{
    public string OwnerType => "LessonMaterial";

    public Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(currentUserId));

    public async Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUserId, out var userId))
        {
            // No resolvable user id — leave the decision to the endpoint's own auth. Historically
            // this policy returned true unconditionally, so keep that for the anonymous/edge case.
            return true;
        }

        var status = await materialsAccess.GetForUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return !status.Restricted;
    }

    public Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(
            !string.IsNullOrEmpty(currentUserId)
            && string.Equals(currentUserId, context.CreatedByUserId, StringComparison.Ordinal));
    }
}
