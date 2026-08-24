using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Scheduling.Data;

public sealed class SchedulingDbInitializer(
    SchedulingDbContext dbContext,
    ILogger<SchedulingDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Scheduling] applied migrations");
        }
    }

    /// <summary>No per-tenant reference data — rooms and non-working days are created by the school
    /// through the API, not pre-populated (same reasoning as People/Curriculum/StudyGroups).</summary>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
