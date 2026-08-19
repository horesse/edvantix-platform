using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.StudyGroups.Data;

public sealed class StudyGroupsDbInitializer(
    StudyGroupsDbContext dbContext,
    ILogger<StudyGroupsDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[StudyGroups] applied migrations");
        }
    }

    /// <summary>No per-tenant reference data — study groups are created by the school through the
    /// API, not pre-populated (same reasoning as People/Curriculum).</summary>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
