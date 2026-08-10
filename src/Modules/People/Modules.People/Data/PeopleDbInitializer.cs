using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.People.Data;

public sealed class PeopleDbInitializer(
    PeopleDbContext dbContext,
    ILogger<PeopleDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[People] applied migrations");
        }
    }

    /// <summary>
    /// People has no per-tenant reference data to seed — students, teachers and guardians
    /// are created by the school through the API, not pre-populated.
    /// </summary>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
