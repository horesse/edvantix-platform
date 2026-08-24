using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Payments.Data;

public sealed class PaymentsDbInitializer(
    PaymentsDbContext dbContext,
    ILogger<PaymentsDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Payments] applied migrations");
        }
    }

    /// <summary>No per-tenant reference data — tariffs are created by the school through the API,
    /// not pre-populated (same reasoning as People/Curriculum/StudyGroups).</summary>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
