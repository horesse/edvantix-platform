using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Curriculum.Data;

public sealed class CurriculumDbInitializer(
    CurriculumDbContext dbContext,
    ILogger<CurriculumDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Curriculum] applied migrations");
        }
    }

    /// <summary>
    /// Curriculum has no per-tenant reference data to seed — subjects and courses are authored
    /// by the school's methodist through the API, not pre-populated.
    /// </summary>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
