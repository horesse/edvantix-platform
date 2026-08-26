using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Curriculum.Data;

public sealed class CurriculumDbInitializer(
    CurriculumDbContext dbContext,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ILogger<CurriculumDbInitializer> logger) : IDbInitializer
{
    /// <summary>
    /// Default direction names seeded into every newly-provisioned school. Kept short and
    /// language-neutral — the school's methodist is expected to rename/extend the tree through
    /// the API right away; this only guarantees the tree isn't empty on day one. Same wording as
    /// the worked example in <see cref="Subject"/>'s own doc comment and
    /// <c>Curriculum.Tests/Domain/SubjectTests.cs</c>, so there's exactly one "canonical" subject
    /// name in the codebase, not two competing ones.
    /// </summary>
    private static readonly string[] DefaultSubjectNames = ["Английский язык", "Математика"];

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Curriculum] applied migrations");
        }
    }

    /// <summary>
    /// Seeds a couple of top-level <see cref="Subject"/> directions so a brand-new school's
    /// curriculum tree isn't empty. This reverses the module's original decision ("Curriculum has
    /// no per-tenant reference data to seed — subjects and courses are authored by the school's
    /// methodist through the API, not pre-populated"): the provisioning step for
    /// [[Multitenancy]] → "Шаги провижининга под новые модули" explicitly calls for a default
    /// direction, mirroring what Identity's <c>IdentityDbInitializer</c> already does for school
    /// roles. Courses themselves are still never pre-populated — only the top of the Subject tree,
    /// which is otherwise a hard prerequisite (<c>CreateCourseCommand</c> requires a
    /// <c>SubjectId</c>) for the methodist's very first action in the API/UI.
    ///
    /// Idempotent by <see cref="Subject.Slug"/> — matches Identity's <c>IdentityDbInitializer</c>
    /// "check before insert" style. Safe to call on every provisioning run / retry.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var sortOrder = 0;
        foreach (var name in DefaultSubjectNames)
        {
            var candidate = Subject.Create(name, parentId: null, sortOrder);
            var exists = await dbContext.Subjects
                .AsNoTracking()
                .AnyAsync(s => s.Slug == candidate.Slug, cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                await dbContext.Subjects.AddAsync(candidate, cancellationToken).ConfigureAwait(false);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Seeding default subject '{SubjectName}' for '{TenantId}' Tenant.",
                        name,
                        multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id);
                }
            }

            sortOrder++;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
